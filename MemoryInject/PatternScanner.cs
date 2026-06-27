using System;

namespace MemoryEngine
{
    public class PatternScanner
    {
        private readonly Engine _engine;

        public PatternScanner(Engine engine) => _engine = engine;

        public IntPtr FindPattern(int regionSize, byte[] pattern)
        {
            byte[] buffer = new byte[regionSize];
            MemoryAccess.ReadProcessMemory(_engine.ProcessHandle, _engine.ModuleBase, buffer, (uint)regionSize, out _);

            for (int i = 0; i < buffer.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (pattern[j] != 0xFF && buffer[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return IntPtr.Add(_engine.ModuleBase, i);
            }
            return IntPtr.Zero;
        }

        public IntPtr DynamicFindPattern(string patternString)
        {
        
            string[] parts = patternString.Split(' ');
            byte[] pattern = new byte[parts.Length];
            bool[] mask = new bool[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "??" || parts[i] == "?")
                {
                    mask[i] = false; 
                }
                else
                {
                    pattern[i] = Convert.ToByte(parts[i], 16);
                    mask[i] = true; 
                }
            }

            byte[] buffer = new byte[_engine.ModuleSize];
            MemoryAccess.ReadProcessMemory(_engine.ProcessHandle, _engine.ModuleBase, buffer, (uint)_engine.ModuleSize, out _);

         
            for (int i = 0; i < buffer.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {

                    if (mask[j] && buffer[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return IntPtr.Add(_engine.ModuleBase, i);
            }
            return IntPtr.Zero;
        }
    }
}