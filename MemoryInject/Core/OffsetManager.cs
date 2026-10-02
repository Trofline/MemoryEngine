using System;
using System.Collections.Generic;

namespace MemoryEngine.Core
{
    public enum OffsetType
    {
        Absolute,       // Feste Adresse
        ModuleRelative, // Basisadresse des Moduls + Offset
        PointerChain,   // Basisadresse + Pointer-Kette (Offsets)
        Pattern         // Wird zur Laufzeit über AOB-Scan aufgelöst
    }

    public class GameOffset
    {
        public string Name { get; set; } = string.Empty;
        public OffsetType Type { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public IntPtr Address { get; set; } = IntPtr.Zero;
        public int[] PointerChain { get; set; } = Array.Empty<int>();
        public string Pattern { get; set; } = string.Empty;
    }

    public class OffsetManager
    {
        private readonly Dictionary<string, GameOffset> _offsets = new(StringComparer.OrdinalIgnoreCase);

        public void AddAbsolute(string name, IntPtr address)
        {
            _offsets[name] = new GameOffset { Name = name, Type = OffsetType.Absolute, Address = address };
        }

        public void AddModuleRelative(string name, string moduleName, IntPtr offset)
        {
            _offsets[name] = new GameOffset { Name = name, Type = OffsetType.ModuleRelative, ModuleName = moduleName, Address = offset };
        }

        public void AddPointerChain(string name, string moduleName, IntPtr baseOffset, int[] chain)
        {
            _offsets[name] = new GameOffset { Name = name, Type = OffsetType.PointerChain, ModuleName = moduleName, Address = baseOffset, PointerChain = chain };
        }

        public void AddPattern(string name, string moduleName, string pattern)
        {
            _offsets[name] = new GameOffset { Name = name, Type = OffsetType.Pattern, ModuleName = moduleName, Pattern = pattern };
        }

        public GameOffset Get(string name)
        {
            if (_offsets.TryGetValue(name, out var offset))
                return offset;

            throw new KeyNotFoundException($"Offset '{name}' not found");
        }

        public bool Contains(string name) => _offsets.ContainsKey(name);

        // --- DIE NEUE RESOLVE-METHODE ---
        // Berechnet die finale IntPtr-Adresse basierend auf dem Offset-Typ
        public IntPtr Resolve(string name, Engine engine)
        {
            var offset = Get(name);

            switch (offset.Type)
            {
                case OffsetType.Absolute:
                    return offset.Address;

                case OffsetType.ModuleRelative:
                    var (baseAddr, _) = engine.GetModuleInfo(offset.ModuleName);
                    return IntPtr.Add(baseAddr, offset.Address.ToInt32());

                case OffsetType.PointerChain:
                    var (modBase, _) = engine.GetModuleInfo(offset.ModuleName);
                    IntPtr basePtr = IntPtr.Add(modBase, offset.Address.ToInt32());
                    return engine.ReadPointerChain(basePtr, offset.PointerChain);

                case OffsetType.Pattern:
                    return engine.PatternScanner.FindPattern(offset.ModuleName, offset.Pattern);

                default:
                    return IntPtr.Zero;
            }
        }
    }
}