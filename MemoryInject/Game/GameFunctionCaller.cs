using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MemoryEngine
{
    public class GameFunctionCaller
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr hObject);

        private readonly Process _process;
        private readonly IntPtr _hProcess;

        public GameFunctionCaller(Process process)
        {
            _process = process;
            // Wir brauchen Zugriff auf den Prozess zum Erstellen von Threads
            _hProcess = OpenProcess(0x1F0FFF, false, process.Id);
        }

        // Ruft eine Funktion ohne Argumente auf (void func())
        public bool CallVoidFunction(IntPtr functionAddress)
        {
            IntPtr hThread = CreateRemoteThread(_hProcess, IntPtr.Zero, 0, functionAddress, IntPtr.Zero, 0, IntPtr.Zero);
            if (hThread != IntPtr.Zero)
            {
                CloseHandle(hThread);
                return true;
            }
            return false;
        }

        ~GameFunctionCaller()
        {
            CloseHandle(_hProcess);
        }
    }
}