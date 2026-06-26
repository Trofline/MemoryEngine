using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MemoryEngine
{
    public class Engine : IDisposable
    {
        public IntPtr ProcessHandle { get; private set; }
        public IntPtr ModuleBase { get; private set; }
        public int ModuleSize { get; private set; }

        // DAS HIER IST DEIN SCHALTER FÜR ALLES
        public bool Is64Bit { get; private set; }

        public Engine(string processName, bool force32Bit = false)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0) throw new Exception($"Process '{processName}' not found!");

            Process proc = processes[0];
            ProcessModule module = proc.MainModule;

            if (module == null) throw new Exception("Main Module not found!");

            ModuleBase = module.BaseAddress;
            ModuleSize = module.ModuleMemorySize;
            ProcessHandle = MemoryAccess.OpenProcess(MemoryAccess.PROCESS_ALL_ACCESS, false, proc.Id);

            // Automatische Erkennung, wenn du es nicht erzwingst
            Is64Bit = !force32Bit && Is64BitProcess(proc);
        }

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);

        private bool Is64BitProcess(Process proc)
        {
            if (!Environment.Is64BitOperatingSystem) return false;
            IsWow64Process(proc.Handle, out bool isWow64);
            return !isWow64; // Wenn es nicht WOW64 ist, ist es ein echter 64-Bit Prozess
        }

        // --- DYNAMISCHE METHODEN (Passen sich an 32/64 Bit an) ---
        public IntPtr ReadPointer(IntPtr address)
        {
            int size = Is64Bit ? 8 : 4; // Hier passiert die Magie!
            byte[] buffer = new byte[size];
            MemoryAccess.ReadProcessMemory(ProcessHandle, address, buffer, (uint)size, out _);

            return Is64Bit ? (IntPtr)BitConverter.ToInt64(buffer, 0)
                           : (IntPtr)BitConverter.ToInt32(buffer, 0);
        }

        public IntPtr ReadPointerChain(IntPtr baseAddress, int[] offsets)
        {
            IntPtr currentAddress = baseAddress;
            for (int i = 0; i < offsets.Length - 1; i++)
            {
                currentAddress = ReadPointer(IntPtr.Add(currentAddress, offsets[i]));
                if (currentAddress == IntPtr.Zero) return IntPtr.Zero;
            }
            return IntPtr.Add(currentAddress, offsets[offsets.Length - 1]);
        }

        // --- STATISCHE METHODEN (Immer 4 Bytes, das ist physikalisch vorgegeben!) ---
        public int ReadInt(IntPtr address)
        {
            byte[] buffer = new byte[4];
            MemoryAccess.ReadProcessMemory(ProcessHandle, address, buffer, 4, out _);
            return BitConverter.ToInt32(buffer, 0); // int ist immer 32 Bit / 4 Bytes!
        }

        public void WriteInt(IntPtr address, int value)
        {
            byte[] data = BitConverter.GetBytes(value);
            MemoryAccess.WriteProcessMemory(ProcessHandle, address, data, (uint)data.Length, out _);
        }

        public float ReadFloat(IntPtr address)
        {
            byte[] buffer = new byte[4];
            MemoryAccess.ReadProcessMemory(ProcessHandle, address, buffer, 4, out _);
            return BitConverter.ToSingle(buffer, 0); // float ist immer 32 Bit / 4 Bytes!
        }

        public byte[] ReadMemory(IntPtr address, int size)
        {
            byte[] buffer = new byte[size];
            MemoryAccess.ReadProcessMemory(ProcessHandle, address, buffer, (uint)size, out _);
            return buffer;
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