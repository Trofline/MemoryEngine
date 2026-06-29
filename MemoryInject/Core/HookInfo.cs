using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryEngine.Core
{
    public class HookInfo
    {
        public IntPtr HookAddress { get; set; }
        public IntPtr CaveAddress { get; set; }
        public byte[]? OriginalBytes { get; set; }
    }
}
