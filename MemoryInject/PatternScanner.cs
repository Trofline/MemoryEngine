using System;

namespace MemoryEngine
{
    public class PatternScanner
    {
        private readonly Engine _engine;

        public PatternScanner(Engine engine) => _engine = engine;

        public IntPtr FindPattern(string moduleName, string patternString)
        {
            var (baseAddress, moduleSize) = _engine.GetModuleInfo(moduleName);

            if (baseAddress == IntPtr.Zero || moduleSize == 0)
                return IntPtr.Zero;

            return FindPattern(baseAddress, moduleSize, patternString);
        }

        public IntPtr FindPattern(IntPtr baseAddress, int regionSize, string patternString)
        {
            string[] parts = patternString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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

            byte[] buffer = new byte[regionSize];
            MemoryAccess.ReadProcessMemory(_engine.ProcessHandle, baseAddress, buffer, (uint)regionSize, out _);

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

                if (match) return IntPtr.Add(baseAddress, i);
            }

            return IntPtr.Zero;
        }

        public IntPtr FindPattern(int regionSize, string patternString)
        {
            return FindPattern(_engine.ModuleBase, regionSize, patternString);
        }
    }
}