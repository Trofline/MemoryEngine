using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryEngine.Core
{
    public enum OffsetType
    {
        Absolute,          // Feste Adresse
        ModuleRelative,    // ModuleBase + Offset
        PointerChain,      // ModuleBase + BaseOffset -> Multilevel Pointer Path
        Pattern,           // Array of Bytes (AOB) Scan
        DynamicExpression, // Dynamische Berechnung via Lambda/Funktion
        InstructionRelative // RIP-Relative Adressierung (z. B. MOV RAX, [RIP + 0x1234])
    }

    /// <summary>
    /// Event-Argumente für Änderungen an gelösten Offsets (z. B. nach neuem Pattern-Scan)
    /// </summary>
    public class OffsetResolvedEventArgs : EventArgs
    {
        public string Name { get; }
        public IntPtr Address { get; }

        public OffsetResolvedEventArgs(string name, IntPtr address)
        {
            Name = name;
            Address = address;
        }
    }

    public class GameOffset
    {
        public string Name { get; set; } = string.Empty;
        public string Group { get; set; } = "Default"; // Gruppierung (z. B. "Player", "EntityList")
        public OffsetType Type { get; set; }
        public string ModuleName { get; set; } = string.Empty;

        [JsonIgnore]
        public IntPtr Address { get; set; } = IntPtr.Zero;

        // JSON-Hilfseigenschaft für die Serialisierung von IntPtr
        public long RawAddress
        {
            get => Address.ToInt64();
            set => Address = new IntPtr(value);
        }

        public int[] PointerChain { get; set; } = Array.Empty<int>();
        public string Pattern { get; set; } = string.Empty;
        public string Mask { get; set; } = string.Empty; // Optional für Masken-basierte Scans (xx??xx)
        public int PatternOffset { get; set; } = 0;      // Additiver Offset nach Pattern-Match
        public int InstructionLength { get; set; } = 0; // Für RIP-Relative Offsets (x64)

        // Nested Struct Offsets (z. B. PlayerBase + HealthOffset)
        public string ParentOffsetName { get; set; } = string.Empty;
        public int StructOffset { get; set; } = 0;

        // Dynamische Evaluation zur Laufzeit
        [JsonIgnore]
        public Func<Engine, IntPtr>? DynamicResolver { get; set; }
    }

    public class OffsetManager
    {
        private readonly ConcurrentDictionary<string, GameOffset> _offsets = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, IntPtr> _resolvedCache = new(StringComparer.OrdinalIgnoreCase);

        public event EventHandler<OffsetResolvedEventArgs>? OnOffsetResolved;

        #region Fluent Builder & Add-Methoden

        public OffsetManager Add(GameOffset offset)
        {
            _offsets[offset.Name] = offset;
            InvalidateCache(offset.Name);
            return this;
        }

        public OffsetManager AddAbsolute(string name, IntPtr address, string group = "Default")
            => Add(new GameOffset { Name = name, Type = OffsetType.Absolute, Address = address, Group = group });

        public OffsetManager AddModuleRelative(string name, string moduleName, IntPtr offset, string group = "Default")
            => Add(new GameOffset { Name = name, Type = OffsetType.ModuleRelative, ModuleName = moduleName, Address = offset, Group = group });

        // Overload für String-Hex-Offsets ("0x1234" oder "1234")
        public OffsetManager AddModuleRelative(string name, string moduleName, string hexOffset, string group = "Default")
        {
            long parsedOffset = hexOffset.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? Convert.ToInt64(hexOffset, 16)
                : Convert.ToInt64(hexOffset, 10);

            return AddModuleRelative(name, moduleName, new IntPtr(parsedOffset), group);
        }

        public OffsetManager AddPointerChain(string name, string moduleName, IntPtr baseOffset, int[] chain, string group = "Default")
            => Add(new GameOffset { Name = name, Type = OffsetType.PointerChain, ModuleName = moduleName, Address = baseOffset, PointerChain = chain, Group = group });

        // Overload für PointerChain mit String-Hex BaseOffset
        public OffsetManager AddPointerChain(string name, string moduleName, string hexBaseOffset, int[] chain, string group = "Default")
        {
            long parsedOffset = hexBaseOffset.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? Convert.ToInt64(hexBaseOffset, 16)
                : Convert.ToInt64(hexBaseOffset, 10);

            return AddPointerChain(name, moduleName, new IntPtr(parsedOffset), chain, group);
        }

        public OffsetManager AddPattern(string name, string moduleName, string pattern, int patternOffset = 0, string mask = "", int instructionLength = 0, string group = "Default")
            => Add(new GameOffset { Name = name, Type = OffsetType.Pattern, ModuleName = moduleName, Pattern = pattern, Mask = mask, PatternOffset = patternOffset, InstructionLength = instructionLength, Group = group });

        // Overload für Pattern ohne expliziten Modulnamen (Standard: leeres Modul/Hauptmodul)
        public OffsetManager AddPattern(string name, string pattern, int patternOffset = 0, string group = "Default")
            => AddPattern(name, string.Empty, pattern, patternOffset: patternOffset, group: group);

        public OffsetManager AddInstructionRelative(string name, string moduleName, string pattern, int ripOffsetPosition, int instructionLength, string group = "Default")
            => Add(new GameOffset { Name = name, Type = OffsetType.InstructionRelative, ModuleName = moduleName, Pattern = pattern, PatternOffset = ripOffsetPosition, InstructionLength = instructionLength, Group = group });

        public OffsetManager AddStructMember(string name, string parentOffsetName, int structOffset, string group = "Default")
            => Add(new GameOffset { Name = name, ParentOffsetName = parentOffsetName, StructOffset = structOffset, Group = group });

        public OffsetManager AddDynamic(string name, Func<Engine, IntPtr> resolver, string group = "Default")
            => Add(new GameOffset { Name = name, Type = OffsetType.DynamicExpression, DynamicResolver = resolver, Group = group });

        #endregion

        #region Resolve Mechanics

        public IntPtr Resolve(string name, Engine engine, bool forceRefresh = false)
        {
            if (!forceRefresh && _resolvedCache.TryGetValue(name, out var cachedAddress) && cachedAddress != IntPtr.Zero)
                return cachedAddress;

            var offset = Get(name);
            IntPtr resolvedPtr = IntPtr.Zero;

            // 1. Auflösung abhängiger Struct-Member Offsets
            if (!string.IsNullOrEmpty(offset.ParentOffsetName))
            {
                IntPtr parentAddr = Resolve(offset.ParentOffsetName, engine, forceRefresh);
                if (parentAddr == IntPtr.Zero)
                    return IntPtr.Zero;

                resolvedPtr = IntPtr.Add(parentAddr, offset.StructOffset);
            }
            else
            {
                // 2. Reguläre Typ-Auflösung
                resolvedPtr = offset.Type switch
                {
                    OffsetType.Absolute =>
                        offset.Address,

                    OffsetType.ModuleRelative =>
                        IntPtr.Add(engine.GetModuleInfo(offset.ModuleName).BaseAddress, offset.Address.ToInt32()),

                    OffsetType.PointerChain =>
                        ResolvePointerChain(engine, offset),

                    OffsetType.Pattern =>
                        ResolvePattern(engine, offset),

                    OffsetType.InstructionRelative =>
                        ResolveInstructionRelative(engine, offset),

                    OffsetType.DynamicExpression =>
                        offset.DynamicResolver?.Invoke(engine) ?? IntPtr.Zero,

                    _ => IntPtr.Zero
                };
            }

            if (resolvedPtr != IntPtr.Zero)
            {
                _resolvedCache[name] = resolvedPtr;
                OnOffsetResolved?.Invoke(this, new OffsetResolvedEventArgs(name, resolvedPtr));
            }

            return resolvedPtr;
        }

        public async Task<Dictionary<string, IntPtr>> ResolveAllAsync(Engine engine, IProgress<float>? progress = null, CancellationToken cancellationToken = default)
        {
            var results = new ConcurrentDictionary<string, IntPtr>();
            var keys = _offsets.Keys.ToList();
            int completed = 0;

            await Task.Run(() =>
            {
                Parallel.ForEach(keys, new ParallelOptions { CancellationToken = cancellationToken }, key =>
                {
                    IntPtr ptr = Resolve(key, engine);
                    results[key] = ptr;

                    int current = Interlocked.Increment(ref completed);
                    progress?.Report((float)current / keys.Count);
                });
            }, cancellationToken);

            return new Dictionary<string, IntPtr>(results);
        }

        #endregion

        #region Helper Resolution Logic

        private IntPtr ResolvePointerChain(Engine engine, GameOffset offset)
        {
            var (modBase, _) = engine.GetModuleInfo(offset.ModuleName);
            if (modBase == IntPtr.Zero) return IntPtr.Zero;

            IntPtr basePtr = IntPtr.Add(modBase, offset.Address.ToInt32());
            return engine.ReadPointerChain(basePtr, offset.PointerChain);
        }

        private IntPtr ResolvePattern(Engine engine, GameOffset offset)
        {
            // Greift direkt auf die (string moduleName, string patternString) Überladung zu.
            // Wildcards ('?' und '??') werden vom PatternScanner automatisch verarbeitet.
            IntPtr found = engine.PatternScanner.FindPattern(offset.ModuleName, offset.Pattern);

            if (found == IntPtr.Zero) return IntPtr.Zero;

            return IntPtr.Add(found, offset.PatternOffset);
        }

        private IntPtr ResolveInstructionRelative(Engine engine, GameOffset offset)
        {
            // Liest 32-Bit RIP-relative Offsets (x64 Instruktionen)
            IntPtr instructionAddr = engine.PatternScanner.FindPattern(offset.ModuleName, offset.Pattern);
            if (instructionAddr == IntPtr.Zero) return IntPtr.Zero;

            IntPtr ripOffsetLocation = IntPtr.Add(instructionAddr, offset.PatternOffset);
            int relativeOffset = engine.Read<int>(ripOffsetLocation);

            long nextInstructionAddr = instructionAddr.ToInt64() + offset.InstructionLength;
            return new IntPtr(nextInstructionAddr + relativeOffset);
        }

        #endregion

        #region Cache & Lookup Management

        public GameOffset Get(string name)
        {
            if (_offsets.TryGetValue(name, out var offset))
                return offset;

            throw new KeyNotFoundException($"Offset '{name}' wurde im OffsetManager nicht gefunden.");
        }

        public bool TryGetResolved(string name, out IntPtr address) => _resolvedCache.TryGetValue(name, out address);

        public bool Contains(string name) => _offsets.ContainsKey(name);

        public void InvalidateCache(string? name = null)
        {
            if (string.IsNullOrEmpty(name))
                _resolvedCache.Clear();
            else
                _resolvedCache.TryRemove(name, out _);
        }

        #endregion

        #region Import & Export (JSON Engine Configs)

        public string ExportToJson(bool indented = true)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = indented,
                Converters = { new JsonStringEnumConverter() }
            };
            return JsonSerializer.Serialize(_offsets.Values, options);
        }

        public void SaveToFile(string filePath) => File.WriteAllText(filePath, ExportToJson());

        public void LoadFromJson(string json)
        {
            var options = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } };
            var list = JsonSerializer.Deserialize<List<GameOffset>>(json, options);

            if (list == null) return;

            foreach (var offset in list)
            {
                Add(offset);
            }
        }

        public void LoadFromFile(string filePath)
        {
            if (File.Exists(filePath))
                LoadFromJson(File.ReadAllText(filePath));
        }

        #endregion
    }
}