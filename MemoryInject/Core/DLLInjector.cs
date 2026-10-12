using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Injector
{
    public class DllInjector
    {
        // --- Windows API Imports ---
        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        // --- Konstanten ---
        const int PROCESS_CREATE_THREAD = 0x0002;
        const int PROCESS_QUERY_INFORMATION = 0x0400;
        const int PROCESS_VM_OPERATION = 0x0008;
        const int PROCESS_VM_WRITE = 0x0020;
        const int PROCESS_VM_READ = 0x0010;
        const uint MEM_COMMIT_RESERVE = 0x00003000;
        const uint PAGE_READWRITE = 0x04;

        public static bool Inject(string processName, string dllPath)
        {
            Process[] targetProcesses = Process.GetProcessesByName(processName);
            if (targetProcesses.Length == 0)
            {
                Console.WriteLine("Proccess not found!");
                return false;
            }

            Process targetProcess = targetProcesses[0];

            // 1. Prozess öffnen mit allen nötigen Rechten
            IntPtr hProcess = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, targetProcess.Id);
            if (hProcess == IntPtr.Zero) return false;

            // 2. Speicher im Zielprozess reservieren (Groß genug für den DLL-Pfad)
            IntPtr allocatedMemory = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)((dllPath.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT_RESERVE, PAGE_READWRITE);

            // 3. Den Pfad zur DLL in den reservierten Speicher schreiben
            byte[] pathBytes = Encoding.Default.GetBytes(dllPath);
            WriteProcessMemory(hProcess, allocatedMemory, pathBytes, (uint)pathBytes.Length, out _);

            // 4. Die Adresse von 'LoadLibraryA' in der kernel32.dll finden
            IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");

            // 5. Einen neuen Thread im Spiel starten, der LoadLibrary aufruft und unseren Pfad übergibt
            IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, allocatedMemory, 0, IntPtr.Zero);

            if (hThread == IntPtr.Zero)
            {
                Console.WriteLine("Error while starting the remote thread.");
                return false;
            }

            CloseHandle(hThread);
            CloseHandle(hProcess);
            return true;
        }
    }
}