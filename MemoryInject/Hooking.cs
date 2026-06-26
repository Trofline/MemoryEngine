using System;
using System.Collections.Generic;
using MemoryEngine.Core;

namespace MemoryEngine.Core
{
    public class Hooking
    {
        private readonly Engine _engine;
        private readonly Dictionary<string, IntPtr> _allocatedCaves = new();
        private readonly Dictionary<string, HookInfo> _activeHooks = new();

        public Hooking(Engine engine) => _engine = engine;

        public IntPtr GetOrCreateCave(string name, int size = 1024)
        {
            // WICHTIG: Erst prüfen, ob schon eine Cave existiert!
            if (_allocatedCaves.TryGetValue(name, out IntPtr existingCave))
                return existingCave;

            IntPtr newCave = MemoryAccess.VirtualAllocEx(_engine.ProcessHandle, IntPtr.Zero, (uint)size,
                MemoryAccess.MEM_COMMIT | MemoryAccess.MEM_RESERVE, MemoryAccess.PAGE_EXECUTE_READWRITE);

            _allocatedCaves[name] = newCave;
            return newCave;
        }

        public void ApplyHook(string featureName, IntPtr hookAddress, string asmScript, bool x32)
        {
            IntPtr cave = GetOrCreateCave(featureName);
            string fullScript = $@"org 0x{cave.ToInt64():X}{Environment.NewLine}{asmScript}";

            byte[] original = _engine.ReadMemory(hookAddress, 11);
            _activeHooks[featureName] = new HookInfo { HookAddress = hookAddress, CaveAddress = cave, OriginalBytes = original };

            byte[] payload;

            if (x32)
            {
                payload = Assembler.Compile32(fullScript);
            }
            else
            {
                payload = Assembler.Compile64(fullScript);
            }
            WriteMemory(cave, payload);
            InjectJMP(hookAddress, cave);
        }


        public void RemoveHook(string featureName)
        {
            if (_activeHooks.TryGetValue(featureName, out var hook))
            {
                WriteMemory(hook.HookAddress, hook.OriginalBytes);
                // WICHTIG: Auch aus dem Cave-Dictionary entfernen
                if (_allocatedCaves.ContainsKey(featureName))
                {
                    MemoryAccess.VirtualFreeEx(_engine.ProcessHandle, _allocatedCaves[featureName], 0, MemoryAccess.MEM_RELEASE);
                    _allocatedCaves.Remove(featureName);
                }
                _activeHooks.Remove(featureName);
            }
        }

        public void InjectJMP(IntPtr source, IntPtr destination)
        {
            byte[] patch = new byte[11];
            for (int i = 0; i < 11; i++) patch[i] = 0x90;
            patch[0] = 0xE9;
            int offset = (int)((long)destination - (long)source - 5);
            Array.Copy(BitConverter.GetBytes(offset), 0, patch, 1, 4);
            WriteMemory(source, patch);
        }
        public void NopMemory(IntPtr address, int length)
        {
            byte[] nops = Enumerable.Repeat((byte)0x90, length).ToArray();
            WriteMemory(address, nops);
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