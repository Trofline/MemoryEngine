using System;
using Binarysharp.Assemblers.Fasm; // Dein gefundener Namespace!

namespace MemoryEngine.Core
{
    public static class Assembler
    {
        public static byte[] Compile32(string assemblyCode)
        {
            try
            {
                // WICHTIG: "use32" erzwingt 32-Bit Assembler für AssaultCube.
                // Das \n ist einfach ein Zeilenumbruch, danach kommt dein Code.
                string fullCode = "use32\n" + assemblyCode;

                // FasmNet ist die Klasse, Assemble ist die Methode
                return FasmNet.Assemble(fullCode);
            }
            catch (Exception ex)
            {
                throw new Exception("FASM Fehler: " + ex.Message);
            }
        }
        public static byte[] Compile64(string assemblyCode)
        {
            try
            {
                // WICHTIG: "use32" erzwingt 32-Bit Assembler für AssaultCube.
                // Das \n ist einfach ein Zeilenumbruch, danach kommt dein Code.
                string fullCode = "use64\n" + assemblyCode;

                // FasmNet ist die Klasse, Assemble ist die Methode
                return FasmNet.Assemble(fullCode);
            }
            catch (Exception ex)
            {
                throw new Exception("FASM Fehler: " + ex.Message);
            }
        }
    }
}