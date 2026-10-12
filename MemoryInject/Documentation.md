==================================================

Created with AI, so please verify all information is correct and up-to-date. This documentation is intended to be beginner-friendly with this package, but it is not a substitute for understanding C#, Windows memory management, or game hacking concepts.
# MemoryEngine - Ultimate Beginner-to-Advanced Documentation

Welcome to MemoryEngine! This document is designed so that anyone—from absolute beginners to experienced developers—can easily understand and use every single class, property, and method in this framework.

---

## Table of Contents

1. Absolute Beginner's Guide (Start Here!)
2. Universal Root (Engine.cs)
3. Core Module
   - OffsetManager.cs & GameOffset.cs
   - PatternScanner.cs
   - MemoryAccess.cs
   - HookInfo.cs
4. External Module
   - ExternalHooking.cs
5. Internal Module
   - InternalHooking.cs & SharedMemoryBridge.cs
6. Game Module
   - ViewMatrix.cs
   - EntityManager.cs (Entity)
   - AimbotMath.cs
   - GameFunctionCaller.cs
7. Overlay Module
   - ESP.cs (ESPOverlay, EnemyScreenData, EspData)

---

## 1. Absolute Beginner's Guide

### What is MemoryEngine?
MemoryEngine is a high-performance C# framework that allows your application to read, modify, freeze, and detour memory, as well as render custom visual overlays (like ESP / Wallhacks) over running computer games.

### Key Concepts in Plain English:
- Process Name: The executable name of the target game (e.g., "ac_client" for AssaultCube).
- Memory Address (IntPtr): A specific location inside the computer's RAM where game information (like Health, Ammo, or Coordinates) is saved.
- Offsets: Rules and calculations used to locate memory addresses dynamically, even if the game restarts or uses ASLR (Address Space Layout Randomization).
- Freeze: Automatically re-writing a value thousands of times a second on a background thread so the game cannot change your Health, Ammo, etc.
- AOB (Array of Bytes) / Pattern: Searching game memory for a unique sequence of assembly bytes (e.g., "48 8B 05 ? ? ? ?") to find dynamic functions or addresses without hardcoding static values.

### How to start any basic project:

Step 1: Add MemoryEngine to your solution and import namespaces:
```csharp
using MemoryEngine;
using MemoryEngine.Core;
using MemoryEngine.Game;
using MemoryEngine.Overlay;
```
Step 2: Connect MemoryEngine to your game process:
```csharp
using var engine = new Engine("ac_client");
```
Step 3: Read or write memory addresses easily!

```csharp
IntPtr healthAddress = new IntPtr(0x050F4F4);
int health = engine.ReadInt32(healthAddress);
engine.WriteInt32(healthAddress, 1337);
```

2. Universal Root (Engine.cs)
The Engine class is the central manager of the framework. It handles process connection, safe memory reading/writing, memory patching/NOPing, value freezing, and subsystem initialization. It implements IDisposable for safe process handle cleanup.

Class Properties:
```csharp
ProcessHandle (IntPtr): Open Windows process handle used for P/Invoke operations (PROCESS_ALL_ACCESS).

ModuleBase (IntPtr): Starting memory address of the game's main executable.

ModuleSize (int): Total size in bytes of the main executable in RAM.

Is64Bit (bool): true if the target process is 64-bit; false if 32-bit.

External (ExternalHooking): Subsystem for external assembly code caves and ICED detours.

Internal (InternalHooking): Subsystem for in-process hooks and delegates.

PatternScanner (PatternScanner): Subsystem for signature pattern scanning.
```
Constructor:
```csharp
public Engine(string processName, bool force32Bit = false)
```
Connects to the specified running game process. Throws an exception if the process is not found or if administrator privileges are required.

Process & Module Methods:
```csharp
public (IntPtr BaseAddress, int ModuleSize) GetModuleInfo(string moduleName)
```
Returns the base address and total size of a loaded module (e.g., "client.dll"). If moduleName is empty, returns the main process module info.

Reading Methods:
```csharp
public byte ReadByte(IntPtr address)

public bool ReadBool(IntPtr address)

public short ReadInt16(IntPtr address)

public ushort ReadUInt16(IntPtr address)

public int ReadInt32(IntPtr address)

public int ReadInt(IntPtr address)

public uint ReadUInt32(IntPtr address)

public long ReadInt64(IntPtr address)

public ulong ReadUInt64(IntPtr address)

public float ReadFloat(IntPtr address)

public double ReadDouble(IntPtr address)

public byte[] ReadMemory(IntPtr address, int size)

public string ReadString(IntPtr address, int length, Encoding? encoding = null)

public IntPtr ReadPointer(IntPtr address) (Reads 4 bytes on x86, 8 bytes on x64)

public IntPtr ReadPointerChain(IntPtr baseAddress, int[] offsets)
Follows a multi-level pointer path to return the final target address.

public T Read(IntPtr address) where T : unmanaged
Reads a full raw C# struct (e.g., Vector3 or custom struct) directly from memory.

Writing Methods:
public void WriteByte(IntPtr address, byte value)

public void WriteBool(IntPtr address, bool value)

public void WriteInt16(IntPtr address, short value)

public void WriteUInt16(IntPtr address, ushort value)

public void WriteInt32(IntPtr address, int value)

public void WriteInt(IntPtr address, int value)

public void WriteUInt32(IntPtr address, uint value)

public void WriteInt64(IntPtr address, long value)

public void WriteUInt64(IntPtr address, ulong value)

public void WriteFloat(IntPtr address, float value)

public void WriteDouble(IntPtr address, double value)

public void WriteString(IntPtr address, string text, Encoding? encoding = null)

public void Write(IntPtr address, T value) where T : unmanaged
```
Utility Methods:
public void Nop(IntPtr address, int length)
Replaces game code with 0x90 (NOP) instructions to disable game routines.

public void Patch(IntPtr address, byte[] bytes)
Temporarily changes memory protection to PAGE_EXECUTE_READWRITE, writes custom bytes, and restores original protection.

Freezing Methods (Cheat Engine Style):
public void FreezeMemory(IntPtr address, byte[] value)

public void FreezeInt(IntPtr address, int value)

public void FreezeFloat(IntPtr address, float value)

public void FreezeBool(IntPtr address, bool value)
Enforces constant values on a background thread running every 10ms.

public void UnfreezeMemory(IntPtr address)
Stops enforcing constant values for the given address.

Cleanup Method:
public void Dispose()
Closes active process handles, clears frozen value threads, and disposes process handles cleanly.

3. Core Module
OffsetManager.cs & GameOffset.cs
Provides a fluent, thread-safe (ConcurrentDictionary) offset management architecture with in-memory caching, JSON serialization, struct nesting, and dynamic evaluation.

Supported Offset Types (OffsetType):
Absolute: Static absolute memory address.

ModuleRelative: Module base address + static offset.

PointerChain: Module base address + base offset followed by a multilevel pointer array.

Pattern: Resolved at runtime via AOB signature scan.

InstructionRelative: RIP-relative addressing for x64 instructions (e.g., MOV RAX, [RIP + 0x1234]).

DynamicExpression: Resolved dynamically at runtime using a C# lambda/delegate.

Builder & Registration Methods:
```csharp
public OffsetManager AddAbsolute(string name, IntPtr address, string group = "Default")

public OffsetManager AddModuleRelative(string name, string moduleName, IntPtr offset, string group = "Default")

public OffsetManager AddModuleRelative(string name, string moduleName, string hexOffset, string group = "Default")

public OffsetManager AddPointerChain(string name, string moduleName, IntPtr baseOffset, int[] chain, string group = "Default")

public OffsetManager AddPointerChain(string name, string moduleName, string hexBaseOffset, int[] chain, string group = "Default")

public OffsetManager AddPattern(string name, string moduleName, string pattern, int patternOffset = 0, string mask = "", int instructionLength = 0, string group = "Default")

public OffsetManager AddPattern(string name, string pattern, int patternOffset = 0, string group = "Default")

public OffsetManager AddInstructionRelative(string name, string moduleName, string pattern, int ripOffsetPosition, int instructionLength, string group = "Default")

public OffsetManager AddStructMember(string name, string parentOffsetName, int structOffset, string group = "Default")

public OffsetManager AddDynamic(string name, Func<Engine, IntPtr> resolver, string group = "Default")

Core Operations:
public IntPtr Resolve(string name, Engine engine, bool forceRefresh = false)

public async Task<Dictionary<string, IntPtr>> ResolveAllAsync(Engine engine, IProgress? progress = null, CancellationToken cancellationToken = default)

public void InvalidateCache(string? name = null)

public string ExportToJson(bool indented = true) / public void SaveToFile(string filePath)

public void LoadFromJson(string json) / public void LoadFromFile(string filePath)

PatternScanner.cs
Scans memory regions or entire process modules for byte signatures containing wildcard masks (? or ??).
```

Methods:
```csharp
public IntPtr FindPattern(string moduleName, string patternString)

public IntPtr FindPattern(IntPtr baseAddress, int regionSize, string patternString)

public IntPtr FindPattern(int regionSize, string patternString)

MemoryAccess.cs
Contains static Win32 P/Invoke declarations for kernel32.dll (OpenProcess, ReadProcessMemory, WriteProcessMemory, VirtualAllocEx, VirtualProtectEx, VirtualFreeEx, CloseHandle).

HookInfo.cs
Data container storing information about an active detour hook:

HookAddress (IntPtr): Target memory location of the original code.

CaveAddress (IntPtr): Allocated code cave where custom assembly code is executed.

OriginalBytes (byte[]): Backup of original overwritten bytes for restoration.
```
4. External Module
ExternalHooking.cs
Provides external assembly detour and code cave capabilities without requiring a DLL injection into the game process. Powered by the Iced x86/x64 disassembler/assembler library.

Methods:
```csharp
public IntPtr GetOrCreateCave(string name, int size = 1024)

public void ApplyDetour(string featureName, IntPtr hookAddress, int instructionLength, Action<Assembler, List> buildCaveCode)

public IntPtr ApplyAobDetour(string featureName, string moduleName, string pattern, int instructionLength, Action<Assembler, List> buildCaveCode)

public void RegisterSymbol(string name, IntPtr address) / public IntPtr GetSymbolAddress(string name)

public IntPtr AllocateVariable(string symbolName, T initialValue = default) where T : unmanaged

public void InjectJMP(IntPtr source, IntPtr destination, int totalLengthToNop = 0)

public void ReturnEarly(IntPtr address)

public void RemoveHook(string featureName)

public void FreeCave(string featureName)

public void RemoveAll()
```
5. Internal Module
InternalHooking.cs & SharedMemoryBridge.cs
Designed for in-process DLL injection scenarios using MinHook and Iced.

SharedMemoryBridge & SharedData:
Struct SharedData: Contains shared execution flags and entity pointer targets.

SharedMemoryBridge: Allocates unmanaged memory bridges (VirtualAlloc) for background worker synchronization.
```csharp
public SharedData Read()

public void ResetFlag()
```
InternalHooking Methods:

public T ApplyStandardHook(IntPtr targetAddress, T detourDelegate) where T : Delegate
Hooks native functions using MinHook and redirects to a C# delegate.

public IntPtr ApplyCodeCave(string caveName, IntPtr hookAddress, int bytesToOverwrite, Action buildCaveCode)
Injects an in-process ICED code cave.

public void ApplyEventCave(string name, IntPtr hookAddress, int bytesToOverwrite, Action<Assembler, uint> buildAsm, Action onTrigger)
Creates a cave that triggers a C# callback on a background worker thread whenever target assembly instructions fire.

6. Game Module
ViewMatrix.cs
Represents a 4x4 matrix structure used to project 3D world points into 2D screen coordinates.

Structs:
MatrixSettings:
```csharp
IsColumnMajor (bool): Enables on-the-fly matrix transposition.

IsZeroToOneRange (bool): True if Z depth ranges from 0 to 1 instead of -1 to 1.

InvertY (bool): True if screen Y coordinates need to be inverted.
```
ViewMatrix:
```csharp
public fixed float m[16]: Fixed float array avoiding GC allocations.

public bool WorldToScreen(Vector3 pos, out Vector2 screenPos, int width, int height)
```
Entity (EntityManager.cs)
Universal entity data model with dynamic custom data extensions:

Properties: Address (IntPtr), Health (int), Position (Vector3), Pitch (float), Yaw (float), Team (int), Name (string).

Computed Helper: IsAlive => Health > 0.

Custom Data Extensions:

public Dictionary<string, object> CustomData

public T GetCustom(string key, T defaultValue = default!)

public void SetCustom(string key, T value)

AimbotMath.cs
Provides 3D vector rotation calculations and target movement prediction.

Methods:
```csharp
public static (float pitch, float yaw) CalculateAngle(Vector3 localPos, Vector3 targetPos, IntPtr entityKey = default, bool prediction = false, float zOffset = 0.5f)
```
Calculates pitch and yaw angles required to target an entity. Supports velocity prediction across frames.

public static void ClearHistory()
Clears historical position tracking data.

GameFunctionCaller.cs
Executes native game functions remotely by spawning threads inside the target process.

Methods:
public GameFunctionCaller(Process process)

public bool CallVoidFunction(IntPtr functionAddress)

7. Overlay Module
ESP.cs (ESPOverlay, EnemyScreenData, EspData)
Hardware-accelerated, click-through, transparent Windows Forms overlay for rendering ESP bounding boxes and labels.

Launch Method:
public static void Launch()
Spawns the transparent overlay on an isolated STA background thread.

Display Data Structures:
EnemyScreenData:

HeadPos (Vector2): 2D screen coordinate for top of entity box.

FeetPos (Vector2): 2D screen coordinate for bottom of entity box.

Distance (float): Distance value displayed under the box.

BoxColor (Color): Bounding box color.

CustomText (string): Optional custom text label (e.g., "Enemy Name").

EspData:

public static readonly object LockObject

public static List Enemies

Updating Overlay Example:
```csharp
var list = new List<EnemyScreenData>
{
    new EnemyScreenData
    {
        HeadPos = new Vector2(960, 300),
        FeetPos = new Vector2(960, 500),
        Distance = 15.0f,
        BoxColor = Color.Red,
        CustomText = "Enemy"
    }
};


lock (EspData.LockObject)
{
    EspData.Enemies = list;
}
```
==================================================