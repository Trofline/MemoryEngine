using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using MinHook;
using Iced.Intel;
using static Iced.Intel.AssemblerRegisters;

namespace MemoryEngine.Internal
{
    // --- DIE DATENSTRUKTUR ---
    [StructLayout(LayoutKind.Sequential)]
    public struct SharedData
    {
        public int Flag;
        public IntPtr Entity;
    }

    // --- DIE BRIDGE (Gekapselt) ---
    public class SharedMemoryBridge : IDisposable
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAlloc(IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualFree(IntPtr lpAddress, uint dwSize, uint dwFreeType);

        public IntPtr Address { get; private set; }
        public SharedMemoryBridge() { Address = VirtualAlloc(IntPtr.Zero, 16, 0x3000, 0x40); }
        public SharedData Read() => Marshal.PtrToStructure<SharedData>(Address);
        public void ResetFlag() => Marshal.WriteInt32(Address, 0);
        public void Dispose() => VirtualFree(Address, 0, 0x8000);
    }

    public class InternalStreamCodeWriter : CodeWriter
    {
        private readonly MemoryStream _stream;
        public InternalStreamCodeWriter(MemoryStream stream) => _stream = stream;
        public override void WriteByte(byte value) => _stream.WriteByte(value);
    }

    // --- DIE HAUPT-ENGINE ---
    public class InternalHooking : IDisposable
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

        private readonly HookEngine _minHookEngine;
        private readonly Dictionary<string, IntPtr> _allocatedCaves = new();
        private readonly List<Delegate> _activeDelegates = new();

        // Internes Event-System
        private class CaveEvent
        {
            public SharedMemoryBridge Bridge = null!;
            public Action<IntPtr> OnTrigger = null!;
        }
        private readonly List<CaveEvent> _eventCaves = new();
        private bool _isRunning = true;
        private Thread _workerThread;

        public InternalHooking()
        {
            _minHookEngine = new HookEngine();
            // Startet die endlose Prüfung magisch im Hintergrund!
            _workerThread = new Thread(EventLoop);
            _workerThread.Start();
        }

        // ==========================================
        // DEIN NEUES POWER-FEATURE: Die Event-Cave
        // ==========================================
        public void ApplyEventCave(string name, IntPtr hookAddress, int bytesToOverwrite, Action<Assembler, uint> buildAsm, Action<IntPtr> onTrigger)
        {
            var bridge = new SharedMemoryBridge();
            _eventCaves.Add(new CaveEvent { Bridge = bridge, OnTrigger = onTrigger });

            // Ruft die normale Cave auf, übergibt aber die Bridge-Adresse
            ApplyCodeCave(name, hookAddress, bytesToOverwrite, (asm) =>
            {
                buildAsm(asm, (uint)bridge.Address);
            });
        }

        // Die "unsichtbare" Hintergrundschleife
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

        // ==========================================
        // ALTE METHODEN (Unverändert für Kompatibilität)
        // ==========================================
        public T ApplyStandardHook<T>(IntPtr targetAddress, T detourDelegate) where T : Delegate
        {
            _activeDelegates.Add(detourDelegate);
            T original = _minHookEngine.CreateHook<T>(targetAddress, detourDelegate);
            _minHookEngine.EnableHook(original);
            return original;
        }

        public IntPtr ApplyCodeCave(string caveName, IntPtr hookAddress, int bytesToOverwrite, Action<Assembler> buildCaveCode)
        {
            IntPtr caveAddr = Marshal.AllocHGlobal(1024);
            VirtualProtect(caveAddr, 1024, 0x40, out _);
            _allocatedCaves[caveName] = caveAddr;

            IntPtr returnAddr = IntPtr.Add(hookAddress, bytesToOverwrite);
            var asm = new Assembler(32);

            buildCaveCode(asm);
            asm.jmp((uint)returnAddr.ToInt32());

            using var stream = new MemoryStream();
            var codeWriter = new InternalStreamCodeWriter(stream);
            asm.Assemble(codeWriter, (ulong)caveAddr.ToInt32());
            byte[] payload = stream.ToArray();

            Marshal.Copy(payload, 0, caveAddr, payload.Length);
            InjectJmpInternal(hookAddress, caveAddr, bytesToOverwrite);

            return caveAddr;
        }

        private void InjectJmpInternal(IntPtr source, IntPtr destination, int lengthToNop)
        {
            List<byte> patch = new List<byte> { 0xE9 };
            patch.AddRange(BitConverter.GetBytes((int)destination - (int)source - 5));
            while (patch.Count < lengthToNop) patch.Add(0x90);

            VirtualProtect(source, (uint)patch.Count, 0x40, out uint oldProtect);
            Marshal.Copy(patch.ToArray(), 0, source, patch.Count);
            VirtualProtect(source, (uint)patch.Count, oldProtect, out _);
        }

        public void Dispose()
        {
            _isRunning = false;
            _minHookEngine.DisableHooks();
            foreach (var cave in _allocatedCaves.Values) Marshal.FreeHGlobal(cave);
            foreach (var caveEvent in _eventCaves) caveEvent.Bridge.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}