using System;
using Binarysharp.Assemblers.Fasm;

namespace MemoryEngine
{
    public static class Assembler
    {
        public static byte[] Compile(string assemblyCode, bool is64Bit)
        {
            try
            {
      
                string mode = is64Bit ? "use64\n" : "use32\n";
                string fullCode = mode + assemblyCode;

                return FasmNet.Assemble(fullCode);
            }
            catch (Exception ex)
            {
                throw new Exception("FASM Fehler: " + ex.Message);
            }
        }
    }
}