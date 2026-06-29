using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MemoryEngine.Core;
using MemoryEngine.External;
using MemoryEngine.Internal;

namespace MemoryEngine
{
    public class Engine : IDisposable
    {
        public IntPtr ProcessHandle { get; private set; }
        public IntPtr ModuleBase { get; private set; }
        public int ModuleSize { get; private set; }
        public bool Is64Bit { get; private set; }

        private readonly Process _proc;
        private readonly Dictionary<IntPtr, byte[]> _frozenValues = new();
        private readonly object _freezeLock = new();
        private bool _isFreezingActive = false;

        public ExternalHooking External { get; private set; }
        public InternalHooking Internal { get; private set; }
        public PatternScanner PatternScanner { get; private set; }

        public Engine(string processName, bool force32Bit = false)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0) throw new Exception($"Process '{processName}' not found!");

            _proc = processes[0];
            ProcessModule? module = _proc.MainModule;

            if (module == null) throw new Exception("Main Module not found!");

            ModuleBase = module.BaseAddress;
            ModuleSize = module.ModuleMemorySize;
            ProcessHandle = MemoryAccess.OpenProcess(MemoryAccess.PROCESS_ALL_ACCESS, false, _proc.Id);

            Is64Bit = !force32Bit && Is64BitProcess(_proc);

            External = new ExternalHooking(this);
            Internal = new InternalHooking();
            PatternScanner = new PatternScanner(this);
        }

        public (IntPtr BaseAddress, int ModuleSize) GetModuleInfo(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName))
                return (ModuleBase, ModuleSize);

            foreach (ProcessModule module in _proc.Modules)
            {
                if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                {
                    return (module.BaseAddress, module.ModuleMemorySize);
                }
            }
            return (IntPtr.Zero, 0);
        }

        // ==========================================
        // UTILITIES (NOP & PATCH)
        // ==========================================

        public void Nop(IntPtr address, int length)
        {
            byte[] nops = Enumerable.Repeat((byte)0x90, length).ToArray();
            Patch(address, nops);
        }

        public void Patch(IntPtr address, byte[] bytes)
        {
            MemoryAccess.VirtualProtectEx(ProcessHandle, address, (uint)bytes.Length, MemoryAccess.PAGE_EXECUTE_READWRITE, out uint oldProtect);
            MemoryAccess.WriteProcessMemory(ProcessHandle, address, bytes, (uint)bytes.Length, out _);
            MemoryAccess.VirtualProtectEx(ProcessHandle, address, (uint)bytes.Length, oldProtect, out _);
        }

        // ==========================================
        // LESE-METHODEN (ALL TYPES)
        // ==========================================

        public byte ReadByte(IntPtr address)
        {
            byte[] buffer = new byte[1];
            MemoryAccess.ReadProcessMemory(ProcessHandle, address, buffer, 1, out _);
            return buffer[0];
        }

        public bool ReadBool(IntPtr address)
        {
            return ReadByte(address) != 0;
        }

        public short ReadInt16(IntPtr address)
        {
            byte[] buffer = new byte[2];
            MemoryAccess.ReadProcessMemory(ProcessHandle, address, buffer, 2, out _);
            return BitConverter.ToInt16(buffer, 0);
        }

        public ushort ReadUInt16(IntPtr address)
        {
            byte[] buffer = new byte[2];
            MemoryAccess.ReadProcessMemory(ProcessHandle, address, buffer, 2, out _);
            return BitConverter.ToUInt16(buffer, 0);
        }

        public int ReadInt32(IntPtr address)
        {
            byte[] buffer = new byte[4];
            MemoryAccess.ReadProcessMemory(ProcessHandle, address, buffer, 4, out _);
            return BitConverter.ToInt32(buffer, 0);
        }

        // ALIAS FÜR EXTERNEN ESP-AUFRUF
        public int ReadInt(IntPtr address)
        {
            return ReadInt32(address);
        }

        public uint ReadUInt32(IntPtr address)
        {
            byte[] buffer = new byte[4];
            MemoryAccess.ReadProcessMemory(ProcessHandle, address, buffer, 4, out _);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public long ReadInt64(IntPtr address)
        {
            byte[] buffer = new byte[8];
            MemoryAccess.ReadProcessMemory(ProcessHandle, address, buffer, 8, out _);
            return BitConverter.ToInt64(buffer, 0);
        }

        public ulong ReadUInt64(IntPtr address)
        {
            byte[] buffer = new byte[8];
            MemoryAccess.ReadProcessMemory(ProcessHandle, address, buffer, 8, out _);
            return BitConverter.ToUInt64(buffer, 0);
        }

        public float ReadFloat(IntPtr address)
        {
            byte[] buffer = new byte[4];
            MemoryAccess.ReadProcessMemory(ProcessHandle, address, buffer, 4, out _);
            return BitConverter.ToSingle(buffer, 0);
        }

        public double ReadDouble(IntPtr address)
        {
            byte[] buffer = new byte[8];
            MemoryAccess.ReadProcessMemory(ProcessHandle, address, buffer, 8, out _);
            return BitConverter.ToDouble(buffer, 0);
        }

        public byte[] ReadMemory(IntPtr address, int size)
        {
            byte[] buffer = new byte[size];
            MemoryAccess.ReadProcessMemory(ProcessHandle, address, buffer, (uint)size, out _);
            return buffer;
        }

        public string ReadString(IntPtr address, int length, Encoding? encoding = null)
        {
            if (encoding == null) encoding = Encoding.UTF8;
            byte[] buffer = ReadMemory(address, length);
            string text = encoding.GetString(buffer);
            int nullIndex = text.IndexOf('\0');
            return nullIndex >= 0 ? text.Substring(0, nullIndex) : text;
        }

        public IntPtr ReadPointer(IntPtr address)
        {
            int size = Is64Bit ? 8 : 4;
            byte[] buffer = new byte[size];
            MemoryAccess.ReadProcessMemory(ProcessHandle, address, buffer, (uint)size, out _);
            return Is64Bit ? (IntPtr)BitConverter.ToInt64(buffer, 0) : (IntPtr)BitConverter.ToInt32(buffer, 0);
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

        // ==========================================
        // SCHREIB-METHODEN (ALL TYPES)
        // ==========================================

        public void WriteByte(IntPtr address, byte value)
        {
            Patch(address, new byte[] { value });
        }

        public void WriteBool(IntPtr address, bool value)
        {
            WriteByte(address, (byte)(value ? 1 : 0));
        }

        public void WriteInt16(IntPtr address, short value)
        {
            Patch(address, BitConverter.GetBytes(value));
        }

        public void WriteUInt16(IntPtr address, ushort value)
        {
            Patch(address, BitConverter.GetBytes(value));
        }

        public void WriteInt32(IntPtr address, int value)
        {
            Patch(address, BitConverter.GetBytes(value));
        }

        // ALIAS FÜR EXTERNEN ESP-AUFRUF
        public void WriteInt(IntPtr address, int value)
        {
            WriteInt32(address, value);
        }

        public void WriteUInt32(IntPtr address, uint value)
        {
            Patch(address, BitConverter.GetBytes(value));
        }

        public void WriteInt64(IntPtr address, long value)
        {
            Patch(address, BitConverter.GetBytes(value));
        }

        public void WriteUInt64(IntPtr address, ulong value)
        {
            Patch(address, BitConverter.GetBytes(value));
        }

        public void WriteFloat(IntPtr address, float value)
        {
            Patch(address, BitConverter.GetBytes(value));
        }

        public void WriteDouble(IntPtr address, double value)
        {
            Patch(address, BitConverter.GetBytes(value));
        }

        public void WriteString(IntPtr address, string text, Encoding? encoding = null)
        {
            if (encoding == null) encoding = Encoding.UTF8;
            byte[] data = encoding.GetBytes(text + "\0");
            Patch(address, data);
        }

        // ==========================================
        // GENERISCHES POWER-FEATURE (FÜR STRUKTUREN)
        // ==========================================

        public T Read<T>(IntPtr address) where T : unmanaged
        {
            int size = Marshal.SizeOf<T>();
            byte[] buffer = new byte[size];
            MemoryAccess.ReadProcessMemory(ProcessHandle, address, buffer, (uint)size, out _);

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        public void Write<T>(IntPtr address, T value) where T : unmanaged
        {
            int size = Marshal.SizeOf<T>();
            byte[] buffer = new byte[size];

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);
            }
            finally
            {
                handle.Free();
            }

            Patch(address, buffer);
        }

        // ==========================================
        // CHEAT ENGINE FREEZE SYSTEM
        // ==========================================

        public void FreezeMemory(IntPtr address, byte[] value)
        {
            lock (_freezeLock)
            {
                _frozenValues[address] = value;
                if (!_isFreezingActive)
                {
                    _isFreezingActive = true;
                    StartFreezeLoop();
                }
            }
        }

        public void FreezeInt(IntPtr address, int value) => FreezeMemory(address, BitConverter.GetBytes(value));
        public void FreezeFloat(IntPtr address, float value) => FreezeMemory(address, BitConverter.GetBytes(value));
        public void FreezeBool(IntPtr address, bool value) => FreezeMemory(address, new byte[] { (byte)(value ? 1 : 0) });

        public void UnfreezeMemory(IntPtr address)
        {
            lock (_freezeLock)
            {
                if (_frozenValues.ContainsKey(address))
                {
                    _frozenValues.Remove(address);
                }
            }
        }

        private void StartFreezeLoop()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    KeyValuePair<IntPtr, byte[]>[] targets;
                    lock (_freezeLock)
                    {
                        if (_frozenValues.Count == 0)
                        {
                            _isFreezingActive = false;
                            break;
                        }
                        targets = _frozenValues.ToArray();
                    }

                    foreach (var target in targets)
                    {
                        MemoryAccess.WriteProcessMemory(ProcessHandle, target.Key, target.Value, (uint)target.Value.Length, out _);
                    }
                    await Task.Delay(10);
                }
            });
        }

        private bool Is64BitProcess(Process proc)
        {
            if (!Environment.Is64BitOperatingSystem) return false;
            IsWow64Process(proc.Handle, out bool isWow64);
            return !isWow64;
        }

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);

        public void Dispose()
        {
            lock (_freezeLock)
            {
                _frozenValues.Clear();
            }
            if (ProcessHandle != IntPtr.Zero)
            {
                MemoryAccess.CloseHandle(ProcessHandle);
                ProcessHandle = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }
    }
}