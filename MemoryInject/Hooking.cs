using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Iced.Intel;
using static Iced.Intel.AssemblerRegisters;

namespace MemoryEngine
{
    public class StreamCodeWriter : CodeWriter
    {
        private readonly MemoryStream _stream;
        public StreamCodeWriter(MemoryStream stream) => _stream = stream;
        public override void WriteByte(byte value) => _stream.WriteByte(value);
    }

    public class Hooking
    {
        private readonly Engine _engine;
        private readonly PatternScanner _scanner;
        private readonly Dictionary<string, IntPtr> _allocatedCaves = new();
        private readonly Dictionary<string, HookInfo> _activeHooks = new();
        private readonly Dictionary<string, IntPtr> _symbolRegistry = new();

        public Hooking(Engine engine)
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
                throw new Exception($"Fehler beim Allokieren der Code-Cave '{name}'.");

            _allocatedCaves[name] = newCave;
            return newCave;
        }

        // ==========================================
        // ICED DETOURS
        // ==========================================

        public IntPtr ApplyAobDetour(string featureName, string moduleName, string pattern, int instructionLength, Action<Assembler, List<Instruction>> buildCaveCode)
        {
            IntPtr address = _scanner.FindPattern(moduleName, pattern);
            if (address == IntPtr.Zero)
                throw new Exception($"AOB-Pattern für '{featureName}' wurde nicht gefunden!");

            ApplyDetour(featureName, address, instructionLength, buildCaveCode);
            return address;
        }

        public void ApplyDetour(string featureName, IntPtr hookAddress, int instructionLength, Action<Assembler, List<Instruction>> buildCaveCode)
        {
            if (_engine.Is64Bit && instructionLength < 14)
                throw new Exception("Für x64 Absolute Jumps müssen mindestens 14 Bytes überschrieben werden!");
            if (!_engine.Is64Bit && instructionLength < 5)
                throw new Exception("Für x86 Relative Jumps müssen mindestens 5 Bytes überschrieben werden!");

            IntPtr cave = GetOrCreateCave(featureName);
            IntPtr returnAddress = IntPtr.Add(hookAddress, instructionLength);

            byte[] originalBytes = _engine.ReadMemory(hookAddress, instructionLength);
            _activeHooks[featureName] = new HookInfo { HookAddress = hookAddress, CaveAddress = cave, OriginalBytes = originalBytes };

            var codeReader = new ByteArrayCodeReader(originalBytes);
            var decoder = Decoder.Create(_engine.Is64Bit ? 64 : 32, codeReader);
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

            // Rufe deine Custom-Logik auf
            buildCaveCode(asm, originalInstructions);

            // KORREKTUR HIER: Wir nutzen asm.AddInstruction() statt asm.Instructions.Add()
            foreach (var instr in originalInstructions)
            {
                asm.AddInstruction(instr);
            }

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

        // ==========================================
        // VARIABLES & SYMBOLS
        // ==========================================

        public void RegisterSymbol(string name, IntPtr address)
        {
            if (_symbolRegistry.ContainsKey(name)) _symbolRegistry[name] = address;
            else _symbolRegistry.Add(name, address);
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

        // ==========================================
        // INJECTIONS & MEMORY WRITERS
        // ==========================================

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
                patch.AddRange(BitConverter.GetBytes((int)((long)destination - (long)source - 5)));
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
    }
}