using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Iced.Intel;
using MinHook;

namespace MemoryEngine.Internal
{
    // ==========================================
    // NEU: DAS C++ GEFÜHL FÜR C# (Pointers & Memory)
    // ==========================================
    public static unsafe class UnsafeMemory
    {
        // Liest jeden Datentyp direkt aus dem Speicher (wie in C++)
        public static T Read<T>(IntPtr address) where T : unmanaged
        {
            return *(T*)address;
        }

        // Schreibt jeden Datentyp direkt in den Speicher
        public static void Write<T>(IntPtr address, T value) where T : unmanaged
        {
            *(T*)address = value;
        }

        // Pointer-Ketten auflösen (z.B. PlayerBase -> Offset1 -> Offset2)
        public static IntPtr ReadPointer(IntPtr baseAddress, params int[] offsets)
        {
            IntPtr current = baseAddress;
            foreach (int offset in offsets)
            {
                // In 32-Bit Spielen (wie vielen älteren Shootern) lesen wir 4 Bytes
                current = (IntPtr)(*(int*)current);
                if (current == IntPtr.Zero) return IntPtr.Zero;
                current = IntPtr.Add(current, offset);
            }
            return current;
        }
    }

    [Flags]
    public enum MemoryProtection : uint { PAGE_EXECUTE_READWRITE = 0x40, PAGE_EXECUTE_READ = 0x20, PAGE_READWRITE = 0x04 }
    [Flags]
    public enum AllocationType : uint { COMMIT = 0x1000, RESERVE = 0x2000 }
    [Flags]
    public enum FreeType : uint { RELEASE = 0x8000 }

    [StructLayout(LayoutKind.Sequential)]
    public struct SharedData
    {
        public int Flag;
        public IntPtr Entity; // Z.B. der Pointer auf den Spieler
    }

    public class SharedMemoryBridge : IDisposable
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAlloc(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualFree(IntPtr lpAddress, uint dwSize, uint dwFreeType);

        public IntPtr Address { get; private set; }
        public SharedMemoryBridge() { Address = VirtualAlloc(IntPtr.Zero, 16, (uint)(AllocationType.COMMIT | AllocationType.RESERVE), (uint)MemoryProtection.PAGE_EXECUTE_READWRITE); }
        public SharedData Read() => Marshal.PtrToStructure<SharedData>(Address);
        public void ResetFlag() => Marshal.WriteInt32(Address, 0);
        public void Dispose() => VirtualFree(Address, 0, (uint)FreeType.RELEASE);
    }

    public class InternalStreamCodeWriter : CodeWriter
    {
        private readonly MemoryStream _stream;
        public InternalStreamCodeWriter(MemoryStream stream) => _stream = stream;
        public override void WriteByte(byte value) => _stream.WriteByte(value);
    }

    public class InternalHooking : IDisposable
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

        private readonly HookEngine _minHookEngine; // Zurück zu DEINER Instanz-Version
        private readonly Dictionary<string, IntPtr> _allocatedCaves = new();
        private readonly List<Delegate> _activeDelegates = new();

        private class CaveEvent { public SharedMemoryBridge Bridge = null!; public Action<IntPtr> OnTrigger = null!; }
        private readonly List<CaveEvent> _eventCaves = new();

        private volatile bool _isRunning = true;
        private Thread _workerThread;

        public InternalHooking()
        {
            // Initialisierung exakt wie in deinem Originalcode
            _minHookEngine = new HookEngine();

            _workerThread = new Thread(EventLoop) { IsBackground = true };
            _workerThread.Start();
        }

        public void ApplyEventCave(string name, IntPtr hookAddress, int bytesToOverwrite, Action<Assembler, uint> buildAsm, Action<IntPtr> onTrigger)
        {
            var bridge = new SharedMemoryBridge();
            _eventCaves.Add(new CaveEvent { Bridge = bridge, OnTrigger = onTrigger });

            ApplyCodeCave(name, hookAddress, bytesToOverwrite, (asm) => buildAsm(asm, (uint)bridge.Address));
        }

        private void EventLoop()
        {
            while (_isRunning)
            {
                foreach (var cave in _eventCaves)
                {
                    var data = cave.Bridge.Read();
                    if (data.Flag == 1)
                    {
                        cave.OnTrigger(data.Entity);
                        cave.Bridge.ResetFlag();
                    }
                }
                Thread.Sleep(1);
            }
        }

        public T ApplyStandardHook<T>(IntPtr targetAddress, T detourDelegate) where T : Delegate
        {
            _activeDelegates.Add(detourDelegate);

            // Deine originale Syntax
            T original = _minHookEngine.CreateHook<T>(targetAddress, detourDelegate);
            _minHookEngine.EnableHook(original);

            return original;
        }

        public IntPtr ApplyCodeCave(string caveName, IntPtr hookAddress, int bytesToOverwrite, Action<Assembler> buildCaveCode)
        {
            IntPtr caveAddr = Marshal.AllocHGlobal(1024);
            VirtualProtect(caveAddr, 1024, (uint)MemoryProtection.PAGE_EXECUTE_READWRITE, out _);
            _allocatedCaves[caveName] = caveAddr;

            IntPtr returnAddr = IntPtr.Add(hookAddress, bytesToOverwrite);
            var asm = new Assembler(IntPtr.Size == 8 ? 64 : 32);

            buildCaveCode(asm);
            asm.jmp((ulong)returnAddr.ToInt64());

            using var stream = new MemoryStream();
            var codeWriter = new InternalStreamCodeWriter(stream);
            asm.Assemble(codeWriter, (ulong)caveAddr.ToInt64());
            byte[] payload = stream.ToArray();

            Marshal.Copy(payload, 0, caveAddr, payload.Length);
            InjectJmpInternal(hookAddress, caveAddr, bytesToOverwrite);

            return caveAddr;
        }

        private void InjectJmpInternal(IntPtr source, IntPtr destination, int lengthToNop)
        {
            List<byte> patch = new List<byte> { 0xE9 };
            int relativeOffset = (int)(destination.ToInt64() - source.ToInt64() - 5);
            patch.AddRange(BitConverter.GetBytes(relativeOffset));

            while (patch.Count < lengthToNop) patch.Add(0x90);

            VirtualProtect(source, (uint)patch.Count, (uint)MemoryProtection.PAGE_EXECUTE_READWRITE, out uint oldProtect);
            Marshal.Copy(patch.ToArray(), 0, source, patch.Count);
            VirtualProtect(source, (uint)patch.Count, oldProtect, out _);
        }

        public void Dispose()
        {
            _isRunning = false;
            if (_workerThread != null && _workerThread.IsAlive) _workerThread.Join(100);

            // Deine originale Syntax
            _minHookEngine.DisableHooks();

            foreach (var cave in _allocatedCaves.Values) Marshal.FreeHGlobal(cave);
            foreach (var caveEvent in _eventCaves) caveEvent.Bridge.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}