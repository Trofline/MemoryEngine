using System;
using System.Diagnostics;
using System.Linq;

namespace MemoryEngine.Core
{
    public class Engine : IDisposable
    {
        public IntPtr ProcessHandle { get; private set; }
        public IntPtr ModuleBase { get; private set; }
        public int ModuleSize { get; private set; }

        public Engine(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0) throw new Exception($"Process '{processName}' not found!");

            Process proc = processes[0];

            ProcessModule module = proc.MainModule;

            if (module == null) throw new Exception("Main Module not found!");

            ModuleBase = module.BaseAddress;
            ModuleSize = module.ModuleMemorySize;

            ProcessHandle = MemoryAccess.OpenProcess(MemoryAccess.PROCESS_ALL_ACCESS, false, proc.Id);
        }
        // In Engine.cs
        public IntPtr ReadPointer(IntPtr address)
        {
            byte[] buffer = new byte[IntPtr.Size]; // IntPtr.Size ist 4 für x86
            MemoryAccess.ReadProcessMemory(ProcessHandle, address, buffer, (uint)IntPtr.Size, out _);
            return (IntPtr)BitConverter.ToInt32(buffer, 0);
        }
        public void WriteInt(IntPtr address, int value)
        {
            byte[] data = BitConverter.GetBytes(value);
            MemoryAccess.WriteProcessMemory(ProcessHandle, address, data, (uint)data.Length, out _);
        }
        public byte[] ReadMemory(IntPtr address, int size)
        {
            byte[] buffer = new byte[size];
            MemoryAccess.ReadProcessMemory(ProcessHandle, address, buffer, (uint)size, out _);
            return buffer;
        }
        public int ReadInt(IntPtr address)
        {
            byte[] buffer = new byte[4];
            MemoryAccess.ReadProcessMemory(ProcessHandle, address, buffer, 4, out _);
            return BitConverter.ToInt32(buffer, 0);
        }
        public IntPtr ReadPointerChain(IntPtr baseAddress, int[] offsets)
        {
            IntPtr currentAddress = baseAddress;

            // Wir gehen durch die Liste der Offsets, bis auf den letzten
            for (int i = 0; i < offsets.Length - 1; i++)
            {
                currentAddress = ReadPointer(IntPtr.Add(currentAddress, offsets[i]));

                // Sicherheitscheck: Wenn ein Pointer in der Kette kaputt ist, abbrechen
                if (currentAddress == IntPtr.Zero)
                    return IntPtr.Zero;
            }

            // Beim letzten Offset lesen wir nicht mehr den Pointer, 
            // sondern addieren den finalen Offset (das ist dann unsere Zieladresse)
            return IntPtr.Add(currentAddress, offsets[offsets.Length - 1]);
        }
        public float ReadFloat(IntPtr address)
        {
            byte[] buffer = new byte[4];
            MemoryAccess.ReadProcessMemory(ProcessHandle, address, buffer, 4, out _);
            return BitConverter.ToSingle(buffer, 0);
        }
        public void Dispose()
        {
            if (ProcessHandle != IntPtr.Zero)
            {
                MemoryAccess.CloseHandle(ProcessHandle);
                ProcessHandle = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }
    }
}