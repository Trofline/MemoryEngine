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
        Absolute,           // Statische absolute Adresse
        ModuleRelative,     // Modul-Basis + statischer Offset
        PointerChain,       // Modul-Basis + Basis-Offset -> Multi-Level Pointer Pfad
        Pattern,            // Sucht dynamisch nach AOB (gibt die genaue Adresse des Patterns zurück)
        DynamicExpression,  // Wird zur Laufzeit über einen Lambda-Ausdruck/Delegate aufgelöst

        // --- Neue intelligente Instruktions-Typen ---
        InstructionAuto,    // SMART: Nutzt automatisch Absolute für 32-Bit und Relative für 64-Bit!
        InstructionAbsolute,// Erzwungen Absolut (Liest die 4 Bytes direkt als Pointer, typisch für x86/32-Bit)
        InstructionRelative // Erzwungen Relativ (RIP-Math: Addr + Length + Offset, typisch für x64/64-Bit)
    }

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
        public string Group { get; set; } = "Default";
        public OffsetType Type { get; set; }
        public string ModuleName { get; set; } = string.Empty;

        [JsonIgnore]
        public IntPtr Address { get; set; } = IntPtr.Zero;

        public long RawAddress
        {
            get => Address.ToInt64();
            set => Address = new IntPtr(value);
        }

        public int[] PointerChain { get; set; } = Array.Empty<int>();
        public string Pattern { get; set; } = string.Empty;
        public string Mask { get; set; } = string.Empty;
        public int PatternOffset { get; set; } = 0;
        public int InstructionLength { get; set; } = 0;

        public string ParentOffsetName { get; set; } = string.Empty;
        public int StructOffset { get; set; } = 0;

        [JsonIgnore]
        public Func<Engine, IntPtr>? DynamicResolver { get; set; }
    }

    public class OffsetManager
    {
        private readonly ConcurrentDictionary<string, GameOffset> _offsets = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, IntPtr> _resolvedCache = new(StringComparer.OrdinalIgnoreCase);

        public event EventHandler<OffsetResolvedEventArgs>? OnOffsetResolved;

        #region Fluent Builder & Add Methods

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

        public OffsetManager AddModuleRelative(string name, string moduleName, string hexOffset, string group = "Default")
        {
            long parsedOffset = hexOffset.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? Convert.ToInt64(hexOffset, 16)
                : Convert.ToInt64(hexOffset, 10);

            return AddModuleRelative(name, moduleName, new IntPtr(parsedOffset), group);
        }

        public OffsetManager AddCustom(string name, int value, string group = "Default")
        {
            return Add(new GameOffset
            {
                Name = name,
                Type = OffsetType.Absolute,
                Address = new IntPtr(value),
                Group = group
            });
        }

        public bool Remove(string name)
        {
            bool removed = _offsets.TryRemove(name, out _);
            InvalidateCache(name);
            return removed;
        }

        public Dictionary<string, IntPtr> ResolveGroup(string group, Engine engine, bool forceRefresh = false)
        {
            var groupResults = new Dictionary<string, IntPtr>();

            foreach (var kvp in _offsets.Where(o => o.Value.Group.Equals(group, StringComparison.OrdinalIgnoreCase)))
            {
                IntPtr resolved = Resolve(kvp.Key, engine, forceRefresh);
                if (resolved != IntPtr.Zero)
                {
                    groupResults[kvp.Key] = resolved;
                }
            }

            return groupResults;
        }

        public bool ValidateOffsets(Engine engine, IEnumerable<string>? offsetNames = null)
        {
            var targets = offsetNames ?? _offsets.Keys;

            foreach (var name in targets)
            {
                if (Resolve(name, engine, forceRefresh: true) == IntPtr.Zero)
                {
                    return false; // Mindestens ein Offset konnte nicht gefunden werden!
                }
            }
            return true;
        }

        public List<string> GetNamesByGroup(string group)
        {
            return _offsets.Where(o => o.Value.Group.Equals(group, StringComparison.OrdinalIgnoreCase))
                           .Select(o => o.Key)
                           .ToList();
        }

        public OffsetManager AddPointerChain(string name, string moduleName, IntPtr baseOffset, int[] chain, string group = "Default")
            => Add(new GameOffset { Name = name, Type = OffsetType.PointerChain, ModuleName = moduleName, Address = baseOffset, PointerChain = chain, Group = group });

        public OffsetManager AddPointerChain(string name, string moduleName, string hexBaseOffset, int[] chain, string group = "Default")
        {
            long parsedOffset = hexBaseOffset.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? Convert.ToInt64(hexBaseOffset, 16)
                : Convert.ToInt64(hexBaseOffset, 10);

            return AddPointerChain(name, moduleName, new IntPtr(parsedOffset), chain, group);
        }

        public OffsetManager AddPattern(string name, string moduleName, string pattern, int patternOffset = 0, string mask = "", int instructionLength = 0, string group = "Default")
            => Add(new GameOffset { Name = name, Type = OffsetType.Pattern, ModuleName = moduleName, Pattern = pattern, Mask = mask, PatternOffset = patternOffset, InstructionLength = instructionLength, Group = group });

        public OffsetManager AddPattern(string name, string pattern, int patternOffset = 0, string group = "Default")
            => AddPattern(name, string.Empty, pattern, patternOffset: patternOffset, group: group);

        // SMART MODE: Wählt automatisch zwischen 32-Bit (Absolute) und 64-Bit (Relative)
        public OffsetManager AddInstructionAuto(string name, string moduleName, string pattern, int patternOffset, int instructionLength, string group = "Default")
            => Add(new GameOffset { Name = name, Type = OffsetType.InstructionAuto, ModuleName = moduleName, Pattern = pattern, PatternOffset = patternOffset, InstructionLength = instructionLength, Group = group });

        // ERZWUNGEN RELATIV: Für 64-Bit (RIP-Math) oder 32-Bit Jumps/Calls
        public OffsetManager AddInstructionRelative(string name, string moduleName, string pattern, int patternOffset, int instructionLength, string group = "Default")
            => Add(new GameOffset { Name = name, Type = OffsetType.InstructionRelative, ModuleName = moduleName, Pattern = pattern, PatternOffset = patternOffset, InstructionLength = instructionLength, Group = group });

        // ERZWUNGEN ABSOLUT: Für direkte 32-Bit Pointer
        public OffsetManager AddInstructionAbsolute(string name, string moduleName, string pattern, int patternOffset, string group = "Default")
            => Add(new GameOffset { Name = name, Type = OffsetType.InstructionAbsolute, ModuleName = moduleName, Pattern = pattern, PatternOffset = patternOffset, Group = group });

        public OffsetManager AddStructMember(string name, string parentOffsetName, int structOffset, string group = "Default")
            => Add(new GameOffset { Name = name, ParentOffsetName = parentOffsetName, StructOffset = structOffset, Group = group });

        public OffsetManager AddDynamic(string name, Func<Engine, IntPtr> resolver, string group = "Default")
            => Add(new GameOffset { Name = name, Type = OffsetType.DynamicExpression, DynamicResolver = resolver, Group = group });

        #endregion

        #region Resolve Mechanics

        public IntPtr Resolve(string name, Engine engine, bool forceRefresh = false)
        {
            var offset = Get(name);

            // Pointer chains and dynamic expressions shift during runtime; do not cache permanently
            bool shouldCache = offset.Type != OffsetType.PointerChain && offset.Type != OffsetType.DynamicExpression;

            if (!forceRefresh && shouldCache && _resolvedCache.TryGetValue(name, out var cachedAddress) && cachedAddress != IntPtr.Zero)
                return cachedAddress;

            IntPtr resolvedPtr = IntPtr.Zero;

            if (!string.IsNullOrEmpty(offset.ParentOffsetName))
            {
                IntPtr parentAddr = Resolve(offset.ParentOffsetName, engine, forceRefresh);
                if (parentAddr == IntPtr.Zero)
                    return IntPtr.Zero;

                resolvedPtr = IntPtr.Add(parentAddr, offset.StructOffset);
            }
            else
            {
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

                    OffsetType.InstructionAuto =>
                        ResolveInstructionAuto(engine, offset),

                    OffsetType.InstructionAbsolute =>
                        ResolveInstructionAbsolute(engine, offset),

                    OffsetType.InstructionRelative =>
                        ResolveInstructionRelative(engine, offset),

                    OffsetType.DynamicExpression =>
                        offset.DynamicResolver?.Invoke(engine) ?? IntPtr.Zero,

                    _ => IntPtr.Zero
                };
            }

            if (resolvedPtr != IntPtr.Zero && shouldCache)
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
            IntPtr found = engine.PatternScanner.FindPattern(offset.ModuleName, offset.Pattern);
            if (found == IntPtr.Zero) return IntPtr.Zero;

            return IntPtr.Add(found, offset.PatternOffset);
        }

        // --- Die neuen smarten Instruktions-Auflöser ---

        private IntPtr ResolveInstructionAuto(Engine engine, GameOffset offset)
        {
            // Smart Check: 64-Bit nutzt Relativ, 32-Bit nutzt Absolut
            if (engine.Is64Bit)
                return ResolveInstructionRelative(engine, offset);
            else
                return ResolveInstructionAbsolute(engine, offset);
        }

        private IntPtr ResolveInstructionAbsolute(Engine engine, GameOffset offset)
        {
            IntPtr instructionAddr = engine.PatternScanner.FindPattern(offset.ModuleName, offset.Pattern);
            if (instructionAddr == IntPtr.Zero) return IntPtr.Zero;

            // Für 32-Bit: Wir lesen den Speicherwert direkt als absolute Adresse aus
            IntPtr valueLocation = IntPtr.Add(instructionAddr, offset.PatternOffset);
            uint absoluteAddress = engine.ReadUInt32(valueLocation);

            return new IntPtr(absoluteAddress);
        }

        private IntPtr ResolveInstructionRelative(Engine engine, GameOffset offset)
        {
            IntPtr instructionAddr = engine.PatternScanner.FindPattern(offset.ModuleName, offset.Pattern);
            if (instructionAddr == IntPtr.Zero) return IntPtr.Zero;

            // Für 64-Bit (RIP): Instruktions-Basis + Länge + ausgelesener relativer Offset
            IntPtr valueLocation = IntPtr.Add(instructionAddr, offset.PatternOffset);
            int relativeOffset = engine.Read<int>(valueLocation);

            long nextInstructionAddr = instructionAddr.ToInt64() + offset.InstructionLength;
            return new IntPtr(nextInstructionAddr + relativeOffset);
        }

        #endregion

        #region Cache & Lookup Management

        public GameOffset Get(string name)
        {
            if (_offsets.TryGetValue(name, out var offset))
                return offset;

            throw new KeyNotFoundException($"Offset '{name}' was not found in the OffsetManager.");
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

        #region Import & Export

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