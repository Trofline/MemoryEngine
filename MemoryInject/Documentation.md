# MemoryEngine - Complete Developer & API Reference Manual

Welcome to the official, exhaustive API documentation for **MemoryEngine**—a high-performance, modular Game Hacking & Memory Manipulation Framework written in C# (.NET 6.0+).

This manual covers **every single class, struct, enum, property, constructor, and method overload** across all framework modules[cite: 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15]. Each entry includes a clear description and a ready-to-use C# code example[cite: 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15].

---

## Table of Contents

- [1. Universal Root (`Engine.cs`)](#1-universal-root-enginecs)
  - [Engine Properties](#engine-properties)
  - [Engine Constructors & Methods](#engine-constructors--methods)
- [2. Core Module (`MemoryEngine.Core`)](#2-core-module-memoryenginecore)
  - [`OffsetType` Enum](#offsettype-enum)
  - [`OffsetResolvedEventArgs` Class](#offsetresolvedeventargs-class)
  - [`GameOffset` Class](#gameoffset-class)
  - [`OffsetManager` Class](#offsetmanager-class)
  - [`PatternScanner` Class](#patternscanner-class)
  - [`CacheManager` Class](#cachemanager-class)
  - [`InputSystem` Static Class](#inputsystem-static-class)
  - [`MemoryAccess` Native Wrapper](#memoryaccess-native-wrapper)
  - [`HookInfo` Class](#hookinfo-class)
- [3. External Module (`MemoryEngine.External`)](#3-external-module-memoryengineexternal)
  - [`StreamCodeWriter` Class](#streamcodewriter-class)
  - [`ExternalHooking` Class](#externalhooking-class)
- [4. Internal Module (`MemoryEngine.Internal`)](#4-internal-module-memoryengineinternal)
  - [`UnsafeMemory` Static Class](#unsafememory-static-class)
  - [Enums & Structs (`MemoryProtection`, `AllocationType`, `FreeType`, `SharedData`)](#enums--structs-memoryprotection-allocationtype-freetype-shareddata)
  - [`SharedMemoryBridge` Class](#sharedmemorybridge-class)
  - [`InternalStreamCodeWriter` Class](#internalstreamcodewriter-class)
  - [`InternalHooking` Class](#internalhooking-class)
- [5. Injector Module (`Injector`)](#5-injector-module-injector)
  - [`DllInjector` Class](#dllinjector-class)
- [6. Game Module (`MemoryEngine.Game`)](#6-game-module-memoryenginegame)
  - [`MatrixSettings` Struct](#matrixsettings-struct)
  - [`ViewMatrix` Struct](#viewmatrix-struct)
  - [`Entity` Class](#entity-class)
  - [`AimbotMath` Static Class](#aimbotmath-static-class)
  - [`GameFunctionCaller` Class](#gamefunctioncaller-class)
- [7. Overlay Module (`MemoryEngine.Overlay`)](#7-overlay-module-memoryengineoverlay)
  - [`EnemyScreenData` Class](#enemyscreendata-class)
  - [`EspData` Static Class](#espdata-static-class)
  - [`ESP` Form Class](#esp-form-class)

---

## 1. Universal Root (`Engine.cs`)

The `Engine` class is the central orchestrator of the framework[cite: 10]. It connects to the target process, handles memory read/write operations, manages memory freezing, provides instruction patching/NOPing, and initializes sub-modules[cite: 10].

### Engine Properties

#### `ProcessHandle` (`IntPtr`)
- **Description:** Gets the open native Windows process handle with `PROCESS_ALL_ACCESS` permissions[cite: 1, 10].
- **Usage:**
```csharp
IntPtr handle = engine.ProcessHandle;
Console.WriteLine($"Process Handle: 0x{handle:X}");
```

#### `ModuleBase` (`IntPtr`)
- **Description:** Gets the base memory address of the main executable module in the target process[cite: 10].
- **Usage:**
```csharp
IntPtr mainModuleBase = engine.ModuleBase;
Console.WriteLine($"Main Module Base Address: 0x{mainModuleBase:X}");
```

#### `ModuleSize` (`int`)
- **Description:** Gets the total memory size in bytes of the main executable module[cite: 10].
- **Usage:**
```csharp
int size = engine.ModuleSize;
Console.WriteLine($"Main Module Size: {size} bytes");
```

#### `Is64Bit` (`bool`)
- **Description:** Gets a boolean value indicating whether the target process is running as a 64-bit process[cite: 10].
- **Usage:**
```csharp
if (engine.Is64Bit)
{
    Console.WriteLine("Target is a 64-bit process.");
}
```

#### `External` (`ExternalHooking`)
- **Description:** Accesses the subsystem for out-of-process assembly injection, ICED detours, code caves, and remote thread execution[cite: 8, 10].
- **Usage:**
```csharp
engine.External.Nop("DisableSub", engine.ModuleBase + 0x1000, 5);
```

#### `Internal` (`InternalHooking`)
- **Description:** Accesses the subsystem for in-process MinHook detours, code caves, and event bridges[cite: 9, 10].
- **Usage:**
```csharp
IntPtr cave = engine.Internal.ApplyCodeCave("Cave1", engine.ModuleBase + 0x2000, 5, asm => asm.nop());
```

#### `PatternScanner` (`PatternScanner`)
- **Description:** Accesses signature scanning utilities for finding Array-of-Bytes (AOB) patterns[cite: 3, 10].
- **Usage:**
```csharp
IntPtr match = engine.PatternScanner.FindPattern("ac_client.exe", "89 77 04");
```

---

### Engine Constructors & Methods

#### `Engine(string processName, bool force32Bit = false)`
- **Description:** Connects to the target game process by name[cite: 10]. Throws exceptions if the process is not found or lacks administrative privileges[cite: 10].
- **Usage:**
```csharp
using var engine = new Engine("ac_client", force32Bit: false);
```

#### `GetModuleInfo(string moduleName)`
- **Description:** Retrieves the base address and total size for a loaded DLL or EXE[cite: 10]. If `moduleName` is empty, returns main module info[cite: 10].
- **Usage:**
```csharp
var (baseAddress, moduleSize) = engine.GetModuleInfo("client.dll");
```

#### `Nop(IntPtr address, int length)`
- **Description:** Overwrites instructions at the specified address with `0x90` (NOP) bytes[cite: 10].
- **Usage:**
```csharp
engine.Nop(engine.ModuleBase + 0x293A1, 5);
```

#### `Patch(IntPtr address, byte[] bytes)`
- **Description:** Changes memory protection to `PAGE_EXECUTE_READWRITE`, writes custom bytes, and restores original protection[cite: 10].
- **Usage:**
```csharp
engine.Patch(engine.ModuleBase + 0x1500, new byte[] { 0x90, 0x90, 0xC3 });
```

#### Read Methods (`ReadByte`, `ReadBool`, `ReadInt16`, `ReadUInt16`, `ReadInt32`, `ReadInt`, `ReadUInt32`, `ReadInt64`, `ReadUInt64`, `ReadFloat`, `ReadDouble`, `ReadMemory`, `ReadString`, `ReadPointer`)
- **Description:** Reads various primitive data types, byte arrays, strings, or architecture-specific pointers from target memory[cite: 10].
- **Usage:**
```csharp
IntPtr addr = engine.ModuleBase + 0x100;

byte b = engine.ReadByte(addr);
bool flag = engine.ReadBool(addr + 0x1);
short s = engine.ReadInt16(addr + 0x2);
ushort us = engine.ReadUInt16(addr + 0x4);
int i = engine.ReadInt32(addr + 0x8); // or engine.ReadInt
uint ui = engine.ReadUInt32(addr + 0xC);
long l = engine.ReadInt64(addr + 0x10);
ulong ul = engine.ReadUInt64(addr + 0x18);
float f = engine.ReadFloat(addr + 0x20);
double d = engine.ReadDouble(addr + 0x28);
byte[] bytes = engine.ReadMemory(addr, 64);
string str = engine.ReadString(addr + 0x50, length: 32);
IntPtr ptr = engine.ReadPointer(addr + 0x80);
```

#### `ReadPointerChain(IntPtr baseAddress, int[] offsets)`
- **Description:** Dereferences a multi-level pointer chain and returns the final calculated address[cite: 10].
- **Usage:**
```csharp
IntPtr targetAddr = engine.ReadPointerChain(engine.ModuleBase + 0x109B74, new int[] { 0x0, 0x14, 0xF8 });
```

#### `Read<T>(IntPtr address) where T : unmanaged`
- **Description:** Reads a full unmanaged C# struct (e.g. `Vector3`) directly from memory[cite: 10].
- **Usage:**
```csharp
using System.Numerics;
Vector3 position = engine.Read<Vector3>(playerAddr + 0x34);
```

#### Write Methods (`WriteByte`, `WriteBool`, `WriteInt16`, `WriteUInt16`, `WriteInt32`, `WriteInt`, `WriteUInt32`, `WriteInt64`, `WriteUInt64`, `WriteFloat`, `WriteDouble`, `WriteString`)
- **Description:** Overwrites primitive values or formatted text in target memory[cite: 10].
- **Usage:**
```csharp
IntPtr addr = playerAddr;

engine.WriteByte(addr, 1);
engine.WriteBool(addr + 0x1, true);
engine.WriteInt16(addr + 0x2, 500);
engine.WriteUInt16(addr + 0x4, 500);
engine.WriteInt32(addr + 0x8, 1000); // or engine.WriteInt
engine.WriteUInt32(addr + 0xC, 1000);
engine.WriteInt64(addr + 0x10, 999999);
engine.WriteUInt64(addr + 0x18, 999999);
engine.WriteFloat(addr + 0x20, 100.0f);
engine.WriteDouble(addr + 0x28, 99.99);
engine.WriteString(addr + 0x50, "Player1");
```

#### `Write<T>(IntPtr address, T value) where T : unmanaged`
- **Description:** Writes an unmanaged C# struct directly into memory[cite: 10].
- **Usage:**
```csharp
using System.Numerics;
engine.Write<Vector3>(playerAddr + 0x34, new Vector3(10f, 20f, 30f));
```

#### Freezing Methods (`FreezeMemory`, `FreezeInt`, `FreezeFloat`, `FreezeBool`, `UnfreezeMemory`)
- **Description:** Continually enforces values in target memory using a background thread running every 10ms[cite: 10].
- **Usage:**
```csharp
engine.FreezeInt(playerAddr + 0xF8, 999);
engine.FreezeFloat(playerAddr + 0x40, 180.0f);
engine.FreezeBool(playerAddr + 0x80, true);
engine.FreezeMemory(playerAddr + 0x90, new byte[] { 0xFF, 0xFF });

// Unfreeze
engine.UnfreezeMemory(playerAddr + 0xF8);
```

#### `Dispose()`
- **Description:** Safely closes process handles and terminates background freeze threads[cite: 10].
- **Usage:**
```csharp
engine.Dispose();
```

---

## 2. Core Module (`MemoryEngine.Core`)

### `OffsetType` Enum
Defines how an offset is evaluated[cite: 2]:
- `Absolute`, `ModuleRelative`, `PointerChain`, `Pattern`, `DynamicExpression`, `InstructionAuto`, `InstructionAbsolute`, `InstructionRelative`[cite: 2].

```csharp
OffsetType type = OffsetType.InstructionAuto;
```

### `OffsetResolvedEventArgs` Class
Arguments emitted by `OnOffsetResolved`[cite: 2].
- Properties: `Name` (`string`), `Address` (`IntPtr`)[cite: 2].

```csharp
var args = new OffsetResolvedEventArgs("Health", new IntPtr(0x50F4F4));
```

### `GameOffset` Class
Data container for game offset definitions[cite: 2].
- Properties: `Name`, `Group`, `Type`, `ModuleName`, `Address`, `RawAddress`, `PointerChain`, `Pattern`, `Mask`, `PatternOffset`, `InstructionLength`, `ParentOffsetName`, `StructOffset`, `DynamicResolver`[cite: 2].

```csharp
var offset = new GameOffset { Name = "Ammo", Type = OffsetType.PointerChain, PointerChain = new int[] { 0x14 } };
```

---

### `OffsetManager` Class

#### Registration Methods (`Add`, `AddAbsolute`, `AddModuleRelative`, `AddCustom`, `AddPointerChain`, `AddPattern`, `AddInstructionAuto`, `AddInstructionRelative`, `AddInstructionAbsolute`, `AddStructMember`, `AddDynamic`)
- **Description:** Fluent methods to register offset rules into a thread-safe registry[cite: 2].
- **Usage:**
```csharp
var manager = new OffsetManager()
    .AddAbsolute("StaticAddr", new IntPtr(0x400000))
    .AddModuleRelative("LocalPlayer", "ac_client.exe", "0x109B74")
    .AddModuleRelative("LocalPlayerPtr", "ac_client.exe", new IntPtr(0x109B74))
    .AddCustom("CustomVal", 100)
    .AddPointerChain("Health", "ac_client.exe", "0x109B74", new[] { 0xF8 })
    .AddPointerChain("Ammo", "ac_client.exe", new IntPtr(0x109B74), new[] { 0x14 })
    .AddPattern("Pat1", "ac_client.exe", "FF 0E 5F 5E", patternOffset: 0)
    .AddPattern("Pat2", "83 EE 01", patternOffset: 0)
    .AddInstructionAuto("SmartInst", "ac_client.exe", "48 8B 05 ? ? ? ?", 3, 7)
    .AddInstructionRelative("RipInst", "ac_client.exe", "48 8D 0D ? ? ? ?", 3, 7)
    .AddInstructionAbsolute("AbsInst", "ac_client.exe", "A1 ? ? ? ?", 1)
    .AddStructMember("HealthOffset", "LocalPlayer", structOffset: 0xF8)
    .AddDynamic("DynAddr", engine => engine.ModuleBase + 0x5000);
```

#### `Remove(string name)`
- **Description:** Removes a registered offset by name[cite: 2].
- **Usage:**
```csharp
bool removed = manager.Remove("CustomVal");
```

#### `Resolve(string name, Engine engine, bool forceRefresh = false)`
- **Description:** Resolves an offset rule to an absolute memory address[cite: 2].
- **Usage:**
```csharp
IntPtr addr = manager.Resolve("Health", engine, forceRefresh: false);
```

#### `ResolveGroup(string group, Engine engine, bool forceRefresh = false)`
- **Description:** Resolves all offsets belonging to a specific group[cite: 2].
- **Usage:**
```csharp
Dictionary<string, IntPtr> groupAddrs = manager.ResolveGroup("Default", engine);
```

#### `ValidateOffsets(Engine engine, IEnumerable<string>? offsetNames = null)`
- **Description:** Validates that targets resolve to non-zero addresses[cite: 2].
- **Usage:**
```csharp
bool isValid = manager.ValidateOffsets(engine, new[] { "Health", "Ammo" });
```

#### `GetNamesByGroup(string group)`
- **Description:** Retrieves names of all offsets registered in a group[cite: 2].
- **Usage:**
```csharp
List<string> names = manager.GetNamesByGroup("Default");
```

#### `ResolveAllAsync(Engine engine, IProgress<float>? progress = null, CancellationToken cancellationToken = default)`
- **Description:** Resolves all registered offsets in parallel with progress reporting[cite: 2].
- **Usage:**
```csharp
var progress = new Progress<float>(p => Console.WriteLine($"Progress: {p*100}%"));
Dictionary<string, IntPtr> all = await manager.ResolveAllAsync(engine, progress);
```

#### Lookup & Cache Methods (`Get`, `TryGetResolved`, `Contains`, `InvalidateCache`)
- **Description:** Inspects registered offset configs and manages resolution caches[cite: 2].
- **Usage:**
```csharp
GameOffset go = manager.Get("Health");
bool inCache = manager.TryGetResolved("Health", out IntPtr cachedAddr);
bool exists = manager.Contains("Health");
manager.InvalidateCache("Health");
```

#### Import & Export Methods (`ExportToJson`, `SaveToFile`, `LoadFromJson`, `LoadFromFile`)
- **Description:** Serializes offset configurations to/from JSON or disk files[cite: 2].
- **Usage:**
```csharp
string json = manager.ExportToJson(indented: true);
manager.SaveToFile("offsets.json");
manager.LoadFromJson(json);
manager.LoadFromFile("offsets.json");
```

---

### `PatternScanner` Class

#### `PatternScanner(Engine engine)`
- **Description:** Constructor initializing scanner with an engine reference[cite: 3].
- **Usage:**
```csharp
var scanner = new PatternScanner(engine);
```

#### `FindPattern(string moduleName, string patternString)`
- **Description:** Scans a loaded process module for an AOB pattern with wildcards (`?` or `??`)[cite: 3].
- **Usage:**
```csharp
IntPtr found = scanner.FindPattern("ac_client.exe", "89 77 04 ? ? 50");
```

#### `FindPattern(IntPtr baseAddress, int regionSize, string patternString)`
- **Description:** Scans a specific memory region for an AOB pattern[cite: 3].
- **Usage:**
```csharp
IntPtr found = scanner.FindPattern(engine.ModuleBase, 0x10000, "83 EE 01");
```

#### `FindPattern(int regionSize, string patternString)`
- **Description:** Scans starting from main module base address for a region size[cite: 3].
- **Usage:**
```csharp
IntPtr found = scanner.FindPattern(0x10000, "83 EE 01");
```

---

### `CacheManager` Class

#### `CacheManager(int defaultExpirationMs = 16)`
- **Description:** Initializes a memory caching manager (~16ms = 60 FPS updates)[cite: 4].
- **Usage:**
```csharp
var cache = new CacheManager(defaultExpirationMs: 16);
```

#### `GetOrUpdate<T>(string key, Func<T> updateFunc, int? overrideExpirationMs = null)`
- **Description:** Fetches cached value or updates via `updateFunc` if expired[cite: 4].
- **Usage:**
```csharp
int hp = cache.GetOrUpdate("hp", () => engine.ReadInt32(playerAddr + 0xF8));
```

#### `Invalidate(string key)`
- **Description:** Immediately invalidates a specific cache entry[cite: 4].
- **Usage:**
```csharp
cache.Invalidate("hp");
```

---

### `InputSystem` Static Class

#### `IsKeyDown(int virtualKeyCode)`
- **Description:** Checks if a key is held down via `user32.dll` `GetAsyncKeyState`[cite: 7].
- **Usage:**
```csharp
if (InputSystem.IsKeyDown(0x57)) { /* W key pressed */ }
```

#### `IsMenuToggled(int menuKeyCode = 0x2D)`
- **Description:** Handles single-press toggle logic for hotkeys (default VK_INSERT `0x2D`)[cite: 7].
- **Usage:**
```csharp
if (InputSystem.IsMenuToggled(0x2D)) { /* Toggle menu */ }
```

---

### `MemoryAccess` Native Wrapper
P/Invoke declarations from `kernel32.dll`[cite: 1]:
- `OpenProcess`, `ReadProcessMemory`, `WriteProcessMemory`, `VirtualAllocEx`, `VirtualProtectEx`, `VirtualFreeEx`, `CloseHandle`[cite: 1].

```csharp
IntPtr hProc = MemoryAccess.OpenProcess(MemoryAccess.PROCESS_ALL_ACCESS, false, procId);
```

---

### `HookInfo` Class
Container holding detour hook information[cite: 6].
- Properties: `HookAddress`, `CaveAddress`, `OriginalBytes`[cite: 6].

```csharp
var info = new HookInfo { HookAddress = addr, CaveAddress = cave, OriginalBytes = bytes };
```

---

## 3. External Module (`MemoryEngine.External`)

### `StreamCodeWriter` Class
Inherits from ICED `CodeWriter` to assemble bytes into a `MemoryStream`[cite: 8].

#### `StreamCodeWriter(MemoryStream stream)` & `WriteByte(byte value)`
- **Usage:**
```csharp
using var ms = new MemoryStream();
var writer = new StreamCodeWriter(ms);
writer.WriteByte(0x90);
```

---

### `ExternalHooking` Class

#### `ExternalHooking(Engine engine)`
- **Description:** Initializes external assembly detour engine[cite: 8].
- **Usage:**
```csharp
var external = new ExternalHooking(engine);
```

#### `GetOrCreateCave(string name, int size = 1024)`
- **Description:** Allocates executable memory in target process or retrieves existing cave[cite: 8].
- **Usage:**
```csharp
IntPtr cave = external.GetOrCreateCave("MyCave", 1024);
```

#### `Dereference(IntPtr baseAddress, params int[] offsets)`
- **Description:** Dereferences a base pointer and follows offset chain[cite: 8].
- **Usage:**
```csharp
IntPtr target = external.Dereference(engine.ModuleBase + 0x109B74, 0x14);
```

#### `ReadString(IntPtr address, int maxLength = 256, Encoding encoding = null)` & `WriteString(IntPtr address, string text, Encoding encoding = null)`
- **Description:** Reads or writes strings out-of-process[cite: 8].
- **Usage:**
```csharp
string str = external.ReadString(playerAddr + 0x225, 32);
external.WriteString(playerAddr + 0x225, "NewName");
```

#### Patching Methods (`Nop`, `ApplyPatch`, `RestorePatch`)
- **Description:** NOPs or patches bytes while preserving original bytes for restoration[cite: 8].
- **Usage:**
```csharp
external.Nop("DisableDec", engine.ModuleBase + 0x293A1, 5);
external.ApplyPatch("Patch1", engine.ModuleBase + 0x1000, new byte[] { 0x90, 0x90 });
external.RestorePatch("DisableDec");
```

#### Detour Methods (`ApplyDetour`, `ApplyAobDetour`)
- **Description:** Hooks assembly instructions out-of-process using ICED assembler[cite: 8].
- **Usage:**
```csharp
using static Iced.Intel.AssemblerRegisters;

external.ApplyDetour("InfAmmo", engine.ModuleBase + 0x293A1, 6, (asm, orig) =>
{
    asm.mov(eax, 999);
});

external.ApplyAobDetour("AobHook", "ac_client.exe", "89 77 04 8B 01", 6, (asm, orig) =>
{
    asm.nop();
});
```

#### `ExecuteShellcode(Action<Assembler> buildCode)`
- **Description:** Allocates memory, executes assembly via `CreateRemoteThread`, and cleans up[cite: 8].
- **Usage:**
```csharp
using static Iced.Intel.AssemblerRegisters;

external.ExecuteShellcode(asm =>
{
    asm.mov(eax, 100);
    asm.add(eax, 200);
});
```

#### Symbol & Variable Management (`RegisterSymbol`, `GetSymbolAddress`, `AllocateVariable`, `InjectJMP`, `ReturnEarly`, `RemoveHook`, `FreeCave`, `RemoveAll`, `WriteMemory`)
- **Description:** Manages symbol tables, allocates variables, injects jumps, and handles cleanup[cite: 8].
- **Usage:**
```csharp
external.RegisterSymbol("Sym1", engine.ModuleBase + 0x1000);
IntPtr symAddr = external.GetSymbolAddress("Sym1");
IntPtr varAddr = external.AllocateVariable<int>("Counter", 100);
external.InjectJMP(engine.ModuleBase + 0x1000, symAddr, totalLengthToNop: 5);
external.ReturnEarly(engine.ModuleBase + 0x1200);
external.RemoveHook("InfAmmo");
external.FreeCave("MyCave");
external.RemoveAll();
external.WriteMemory(engine.ModuleBase + 0x200, new byte[] { 0x90 });
```

---

## 4. Internal Module (`MemoryEngine.Internal`)

### `UnsafeMemory` Static Class

#### `Read<T>(IntPtr address)` & `Write<T>(IntPtr address, T value)`
- **Description:** High-speed in-process direct pointer reading and writing[cite: 9].
- **Usage:**
```csharp
using MemoryEngine.Internal;

int hp = UnsafeMemory.Read<int>(new IntPtr(0x50F4F4));
UnsafeMemory.Write<int>(new IntPtr(0x50F4F4), 999);
```

#### `ReadPointer(IntPtr baseAddress, params int[] offsets)`
- **Description:** Dereferences pointer chains directly in-process[cite: 9].
- **Usage:**
```csharp
IntPtr target = UnsafeMemory.ReadPointer(baseAddr, 0x14, 0xF8);
```

---

### Enums & Structs (`MemoryProtection`, `AllocationType`, `FreeType`, `SharedData`)
Native allocation flags and shared memory data structures (`Flag`, `Entity`)[cite: 9].

```csharp
var data = new SharedData { Flag = 1, Entity = new IntPtr(0x50F4F4) };
```

---

### `SharedMemoryBridge` Class

#### `SharedMemoryBridge()`, `Address`, `Read()`, `ResetFlag()`, `Dispose()`
- **Description:** Allocates executable shared memory for IPC background event triggers[cite: 9].
- **Usage:**
```csharp
using var bridge = new SharedMemoryBridge();
IntPtr addr = bridge.Address;
SharedData sd = bridge.Read();
bridge.ResetFlag();
```

---

### `InternalStreamCodeWriter` Class
Internal ICED `CodeWriter` writing to `MemoryStream`[cite: 9].

```csharp
using var ms = new MemoryStream();
var writer = new InternalStreamCodeWriter(ms);
writer.WriteByte(0x90);
```

---

### `InternalHooking` Class

#### `InternalHooking()`
- **Description:** Initializes MinHook engine and event thread[cite: 9].
- **Usage:**
```csharp
using var internalHooking = new InternalHooking();
```

#### `ApplyStandardHook<T>(IntPtr targetAddress, T detourDelegate) where T : Delegate`
- **Description:** Hooks function address using MinHook[cite: 9].
- **Usage:**
```csharp
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate int TargetFunc(int a, int b);

TargetFunc orig = internalHooking.ApplyStandardHook<TargetFunc>(
    targetAddr,
    (a, b) => a + b
);
```

#### `ApplyCodeCave(string caveName, IntPtr hookAddress, int bytesToOverwrite, Action<Assembler> buildCaveCode)`
- **Description:** Injects an in-process ICED assembly code cave[cite: 9].
- **Usage:**
```csharp
IntPtr cave = internalHooking.ApplyCodeCave("Cave1", targetAddr, 5, asm => asm.nop());
```

#### `ApplyEventCave(string name, IntPtr hookAddress, int bytesToOverwrite, Action<Assembler, uint> buildAsm, Action<IntPtr> onTrigger)`
- **Description:** Code cave that triggers a C# callback on a background worker thread[cite: 9].
- **Usage:**
```csharp
internalHooking.ApplyEventCave("Event1", targetAddr, 5, (asm, bridgeAddr) =>
{
    asm.mov(eax, 1);
}, (ent) =>
{
    Console.WriteLine($"Event triggered for 0x{ent:X}");
});
```

#### `Dispose()`
- **Description:** Disables MinHook hooks, stops worker thread, and frees caves[cite: 9].
- **Usage:**
```csharp
internalHooking.Dispose();
```

---

## 5. Injector Module (`Injector`)

### `DllInjector` Class

#### `DllInjector.Inject(string processName, string dllPath)`
- **Description:** Injects target DLL into target process using `LoadLibraryA` remote thread creation[cite: 5].
- **Usage:**
```csharp
using Injector;
bool injected = DllInjector.Inject("ac_client", @"C:\Path\To\Payload.dll");
```

---

## 6. Game Module (`MemoryEngine.Game`)

### `MatrixSettings` Struct
Configures matrix projection parameters (`IsColumnMajor`, `IsZeroToOneRange`, `InvertY`)[cite: 14].

```csharp
var settings = new MatrixSettings { IsColumnMajor = true, InvertY = true };
```

---

### `ViewMatrix` Struct
Fixed float array matrix (`fixed float m[16]`)[cite: 14].

#### `WorldToScreen(Vector3 pos, out Vector2 screenPos, int width, int height)`
- **Description:** Projects 3D world coordinates to 2D screen coordinates without GC allocations[cite: 14].
- **Usage:**
```csharp
using System.Numerics;
using MemoryEngine.Game;

var matrix = new ViewMatrix { Settings = new MatrixSettings { IsColumnMajor = true, InvertY = true } };
Vector3 target3D = new Vector3(10f, 20f, 0f);

if (matrix.WorldToScreen(target3D, out Vector2 screen2D, 1920, 1080))
{
    Console.WriteLine($"Screen X: {screen2D.X}, Y: {screen2D.Y}");
}
```

---

### `Entity` Class (`EntityManager.cs`)
Universal entity model (`Address`, `Health`, `X`, `Y`, `Z`, `Position`, `Team`, `Name`, `Pitch`, `Yaw`, `IsAlive`, `CustomData`)[cite: 12].

#### `GetCustom<T>(string key, T defaultValue = default!)` & `SetCustom<T>(string key, T value)`
- **Description:** Gets or sets extended dynamic properties[cite: 12].
- **Usage:**
```csharp
var ent = new Entity();
ent.SetCustom("Armor", 100);
int armor = ent.GetCustom<int>("Armor", 0);
```

---

### `AimbotMath` Static Class

#### `CalculateAngle(Vector3 localPos, Vector3 targetPos, IntPtr entityKey = default, bool prediction = false, float zOffset = 0.5f)`
- **Description:** Calculates Pitch and Yaw view angles with velocity prediction support[cite: 15].
- **Usage:**
```csharp
using System.Numerics;
using MemoryEngine.Game;

var (pitch, yaw) = AimbotMath.CalculateAngle(new Vector3(0,0,0), new Vector3(10,10,0), entityKey: (IntPtr)0x123, prediction: true);
```

#### `ClearHistory()`
- **Description:** Clears velocity tracking history[cite: 15].
- **Usage:**
```csharp
AimbotMath.ClearHistory();
```

---

### `GameFunctionCaller` Class

#### `GameFunctionCaller(Process process)` & `CallVoidFunction(IntPtr functionAddress)`
- **Description:** Calls parameterless `void func()` inside target process via remote thread[cite: 13].
- **Usage:**
```csharp
using System.Diagnostics;
Process proc = Process.GetProcessesByName("ac_client")[0];
var caller = new GameFunctionCaller(proc);
bool called = caller.CallVoidFunction(new IntPtr(0x0040E100));
```

---

## 7. Overlay Module (`MemoryEngine.Overlay`)

### `EnemyScreenData` Class
Rendering properties (`HeadPos`, `FeetPos`, `Distance`, `BoxColor`, `CustomText`)[cite: 11].

```csharp
var enemy = new EnemyScreenData { HeadPos = new Vector2(100, 100), FeetPos = new Vector2(100, 200), Distance = 10f, BoxColor = Color.Red, CustomText = "Enemy" };
```

---

### `EspData` Static Class
Thread-safe rendering container (`LockObject`, `Enemies`)[cite: 11].

```csharp
lock (EspData.LockObject)
{
    EspData.Enemies = new List<EnemyScreenData> { enemy };
}
```

---

### `ESP` Form Class

#### `ESP.Launch()`
- **Description:** Spawns click-through transparent WinForms overlay on background STA thread[cite: 11].
- **Usage:**
```csharp
using MemoryEngine.Overlay;
ESP.Launch();
```

---

## 📜 License

Licensed under the [MIT License](LICENSE).