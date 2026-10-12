using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Iced.Intel;
using MemoryEngine.Core;
using static Iced.Intel.AssemblerRegisters;

namespace MemoryEngine.External
{
    public class StreamCodeWriter : CodeWriter
    {
        private readonly MemoryStream _stream;
        public StreamCodeWriter(MemoryStream stream) => _stream = stream;
        public override void WriteByte(byte value) => _stream.WriteByte(value);
    }

    public class ExternalHooking
    {
        private readonly Engine _engine;
        private readonly PatternScanner _scanner;
        private readonly Dictionary<string, IntPtr> _allocatedCaves = new();
        private readonly Dictionary<string, HookInfo> _activeHooks = new();
        private readonly Dictionary<string, IntPtr> _symbolRegistry = new();
        private readonly Dictionary<string, (IntPtr Address, byte[] OriginalBytes)> _appliedPatches = new();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        public ExternalHooking(Engine engine)
        {
            _engine = engine;
            _scanner = new PatternScanner(engine);
        }

        public IntPtr GetOrCreateCave(string name, int size = 1024)
        {
            if (_allocatedCaves.TryGetValue(name, out IntPtr existingCave))
                return existingCave;

            IntPtr newCave = MemoryAccess.VirtualAllocEx(_engine.ProcessHandle, IntPtr.Zero, (uint)size,
                MemoryAccess.MEM_COMMIT | MemoryAccess.MEM_RESERVE, MemoryAccess.PAGE_EXECUTE_READWRITE);

            if (newCave == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to allocate code cave '{name}'.");

            _allocatedCaves[name] = newCave;
            return newCave;
        }

        #region Memory Reading & Pointers

        /// <summary>
        /// Dereferenziert eine Adresse und folgt optional einer Kette von Offsets (Multi-Level Pointer).
        /// </summary>
        public IntPtr Dereference(IntPtr baseAddress, params int[] offsets)
        {
            IntPtr currentAddress = baseAddress;

            if (offsets == null || offsets.Length == 0)
            {
                return _engine.Is64Bit
                    ? (IntPtr)_engine.Read<long>(currentAddress)
                    : (IntPtr)_engine.Read<int>(currentAddress);
            }

            for (int i = 0; i < offsets.Length; i++)
            {
                currentAddress = _engine.Is64Bit
                    ? (IntPtr)_engine.Read<long>(currentAddress)
                    : (IntPtr)_engine.Read<int>(currentAddress);

                if (currentAddress == IntPtr.Zero)
                    return IntPtr.Zero;

                currentAddress = IntPtr.Add(currentAddress, offsets[i]);
            }

            return currentAddress;
        }

        #endregion

        #region String Manipulation

        /// <summary>
        /// Liest einen null-terminierten String aus dem Speicher.
        /// </summary>
        public string ReadString(IntPtr address, int maxLength = 256, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            byte[] buffer = _engine.ReadMemory(address, maxLength);

            int nullIndex = Array.IndexOf(buffer, (byte)0);
            if (nullIndex >= 0)
            {
                return encoding.GetString(buffer, 0, nullIndex);
            }

            return encoding.GetString(buffer);
        }

        /// <summary>
        /// Schreibt einen String inklusive Null-Terminierung in den Speicher.
        /// </summary>
        public void WriteString(IntPtr address, string text, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            byte[] stringBytes = encoding.GetBytes(text);

            byte[] buffer = new byte[stringBytes.Length + 1];
            Array.Copy(stringBytes, buffer, stringBytes.Length);

            WriteMemory(address, buffer);
        }

        #endregion

        #region NOP & Simple Patching

        /// <summary>
        /// Überschreibt Anweisungen mit NOPs (0x90) und sichert den Originalzustand für ein spätes Restore.
        /// </summary>
        public void Nop(string patchName, IntPtr address, int length)
        {
            byte[] nopArray = new byte[length];
            for (int i = 0; i < length; i++) nopArray[i] = 0x90;

            ApplyPatch(patchName, address, nopArray);
        }

        /// <summary>
        /// Wendet einen Byte-Patch an und speichert die originalen Bytes.
        /// </summary>
        public void ApplyPatch(string patchName, IntPtr address, byte[] newBytes)
        {
            if (!_appliedPatches.ContainsKey(patchName))
            {
                byte[] original = _engine.ReadMemory(address, newBytes.Length);
                _appliedPatches[patchName] = (address, original);
            }

            WriteMemory(address, newBytes);
        }

        /// <summary>
        /// Stellt den Originalzustand eines mit Nop/ApplyPatch modifizierten Speicherbereichs wieder her.
        /// </summary>
        public void RestorePatch(string patchName)
        {
            if (_appliedPatches.TryGetValue(patchName, out var patch))
            {
                WriteMemory(patch.Address, patch.OriginalBytes);
                _appliedPatches.Remove(patchName);
            }
        }

        #endregion

        #region Iced Detours

        public IntPtr ApplyAobDetour(
            string featureName,
            string moduleName,
            string pattern,
            int instructionLength,
            Action<Assembler, List<Instruction>> buildCaveCode,
            bool includeOriginal = true,
            int ripOffset = 0,
            int ripLength = 0)
        {
            IntPtr address = _scanner.FindPattern(moduleName, pattern);
            if (address == IntPtr.Zero)
                throw new InvalidOperationException($"AOB pattern for '{featureName}' was not found!");

            ApplyDetour(featureName, address, instructionLength, buildCaveCode, includeOriginal, ripOffset, ripLength);
            return address;
        }

        public void ApplyDetour(
            string featureName,
            IntPtr hookAddress,
            int instructionLength,
            Action<Assembler, List<Instruction>> buildCaveCode,
            bool includeOriginal = true,
            int ripOffset = 0,
            int ripLength = 0)
        {
            if (hookAddress == IntPtr.Zero)
                throw new ArgumentNullException(nameof(hookAddress), $"Hook address for '{featureName}' is invalid (IntPtr.Zero).");

            if (_engine.Is64Bit && instructionLength < 14)
                throw new ArgumentException("x64 absolute jumps require overwriting at least 14 bytes!");
            if (!_engine.Is64Bit && instructionLength < 5)
                throw new ArgumentException("x86 relative jumps require overwriting at least 5 bytes!");

            IntPtr cave = GetOrCreateCave(featureName);
            IntPtr returnAddress = IntPtr.Add(hookAddress, instructionLength);

            byte[] originalBytes = _engine.ReadMemory(hookAddress, instructionLength);
            _activeHooks[featureName] = new HookInfo { HookAddress = hookAddress, CaveAddress = cave, OriginalBytes = originalBytes };

            var codeReader = new ByteArrayCodeReader(originalBytes);
            var decoder = Iced.Intel.Decoder.Create(_engine.Is64Bit ? 64 : 32, codeReader);
            decoder.IP = (ulong)hookAddress;

            var originalInstructions = new List<Instruction>();
            int decodedLength = 0;
            while (decodedLength < instructionLength)
            {
                var instr = decoder.Decode();
                originalInstructions.Add(instr);
                decodedLength += instr.Length;
            }

            var asm = new Assembler(_engine.Is64Bit ? 64 : 32);

            // Eigenen Code ausführen
            buildCaveCode(asm, originalInstructions);

            // Ripping-Logik für originale Instruktionen
            if (includeOriginal)
            {
                int currentByteOffset = 0;
                foreach (var instr in originalInstructions)
                {
                    bool startsWithinWindow = currentByteOffset >= ripOffset;
                    bool endsWithinWindow = ripLength <= 0 || (currentByteOffset + instr.Length) <= (ripOffset + ripLength);

                    if (startsWithinWindow && endsWithinWindow)
                    {
                        asm.AddInstruction(instr);
                    }
                    currentByteOffset += instr.Length;
                }
            }

            // Rücksprung generieren
            if (_engine.Is64Bit)
            {
                asm.mov(rax, returnAddress.ToInt64());
                asm.jmp(rax);
            }
            else
            {
                asm.push(returnAddress.ToInt32());
                asm.ret();
            }

            using var stream = new MemoryStream();
            var codeWriter = new StreamCodeWriter(stream);
            asm.Assemble(codeWriter, (ulong)cave.ToInt64());

            byte[] payload = stream.ToArray();
            WriteMemory(cave, payload);

            InjectJMP(hookAddress, cave, instructionLength);
        }

        #endregion

        #region Shellcode Execution

        /// <summary>
        /// Allokiert temporären Speicher, führt Assembler-Code in einem neuen Thread aus und räumt danach auf.
        /// </summary>
        public void ExecuteShellcode(Action<Assembler> buildCode)
        {
            var asm = new Assembler(_engine.Is64Bit ? 64 : 32);
            buildCode(asm);
            asm.ret();

            using var stream = new MemoryStream();
            var codeWriter = new StreamCodeWriter(stream);

            IntPtr cave = MemoryAccess.VirtualAllocEx(_engine.ProcessHandle, IntPtr.Zero, 1024,
                MemoryAccess.MEM_COMMIT | MemoryAccess.MEM_RESERVE, MemoryAccess.PAGE_EXECUTE_READWRITE);

            if (cave == IntPtr.Zero)
                throw new InvalidOperationException("Failed to allocate memory for shellcode execution.");

            asm.Assemble(codeWriter, (ulong)cave.ToInt64());
            byte[] payload = stream.ToArray();

            WriteMemory(cave, payload);

            IntPtr threadHandle = CreateRemoteThread(_engine.ProcessHandle, IntPtr.Zero, 0, cave, IntPtr.Zero, 0, out _);

            if (threadHandle != IntPtr.Zero)
            {
                WaitForSingleObject(threadHandle, 2000);
            }

            MemoryAccess.VirtualFreeEx(_engine.ProcessHandle, cave, 0, MemoryAccess.MEM_RELEASE);
        }

        #endregion

        #region Symbols & Injections

        public void RegisterSymbol(string name, IntPtr address)
        {
            _symbolRegistry[name] = address;
        }

        public IntPtr GetSymbolAddress(string name)
        {
            return _symbolRegistry.TryGetValue(name, out IntPtr address) ? address : IntPtr.Zero;
        }

        public IntPtr AllocateVariable<T>(string symbolName, T initialValue = default) where T : unmanaged
        {
            int size = Marshal.SizeOf<T>();
            IntPtr varAddress = MemoryAccess.VirtualAllocEx(_engine.ProcessHandle, IntPtr.Zero, (uint)size,
                MemoryAccess.MEM_COMMIT | MemoryAccess.MEM_RESERVE, MemoryAccess.PAGE_EXECUTE_READWRITE);

            _engine.Write<T>(varAddress, initialValue);
            RegisterSymbol(symbolName, varAddress);
            return varAddress;
        }

        public void InjectJMP(IntPtr source, IntPtr destination, int totalLengthToNop = 0)
        {
            List<byte> patch = new List<byte>();
            if (_engine.Is64Bit)
            {
                patch.AddRange(new byte[] { 0xFF, 0x25, 0x00, 0x00, 0x00, 0x00 });
                patch.AddRange(BitConverter.GetBytes(destination.ToInt64()));
            }
            else
            {
                patch.Add(0xE9);
                int relativeJump = (int)(destination.ToInt64() - source.ToInt64() - 5);
                patch.AddRange(BitConverter.GetBytes(relativeJump));
            }

            while (patch.Count < totalLengthToNop) patch.Add(0x90);
            WriteMemory(source, patch.ToArray());
        }

        public void RemoveAll()
        {
            foreach (var name in _activeHooks.Keys.ToList())
            {
                RemoveHook(name);
            }

            foreach (var name in _appliedPatches.Keys.ToList())
            {
                RestorePatch(name);
            }

            foreach (var name in _allocatedCaves.Keys.ToList())
            {
                FreeCave(name);
            }

            _symbolRegistry.Clear();
        }

        public void ReturnEarly(IntPtr address) => WriteMemory(address, new byte[] { 0xC3 });

        public void RemoveHook(string featureName)
        {
            if (_activeHooks.TryGetValue(featureName, out var hook))
            {
                WriteMemory(hook.HookAddress, hook.OriginalBytes);
                if (_allocatedCaves.ContainsKey(featureName))
                {
                    MemoryAccess.VirtualFreeEx(_engine.ProcessHandle, _allocatedCaves[featureName], 0, MemoryAccess.MEM_RELEASE);
                    _allocatedCaves.Remove(featureName);
                }
                _activeHooks.Remove(featureName);
            }
        }

        public void WriteMemory(IntPtr address, byte[] data)
        {
            MemoryAccess.VirtualProtectEx(_engine.ProcessHandle, address, (uint)data.Length, MemoryAccess.PAGE_EXECUTE_READWRITE, out uint oldProtect);
            MemoryAccess.WriteProcessMemory(_engine.ProcessHandle, address, data, (uint)data.Length, out _);
            MemoryAccess.VirtualProtectEx(_engine.ProcessHandle, address, (uint)data.Length, oldProtect, out _);
        }

        public void FreeCave(string featureName)
        {
            if (_allocatedCaves.TryGetValue(featureName, out IntPtr cave))
            {
                MemoryAccess.VirtualFreeEx(_engine.ProcessHandle, cave, 0, MemoryAccess.MEM_RELEASE);
                _allocatedCaves.Remove(featureName);
            }
        }

        #endregion
    }
}