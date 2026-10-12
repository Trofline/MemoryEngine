# MemoryEngine 🚀

[![NuGet Version](https://img.shields.io/nuget/v/MemoryEngine.svg)](https://www.nuget.org/packages/MemoryEngine)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A high-performance, modular **Game Hacking & Memory Manipulation Framework** written in C# (.NET 6.0+). Supports internal & external memory access, thread-safe offset management, pattern scanning, ICED detours, MinHook hooks, event caves, shellcode execution, DLL injection, input handling, memory caching, and Windows Forms ESP overlays.

---

## 🛠 Features & Complete API Surface

### Engine (`Engine.cs`)[cite: 10]
- **Process Connection & Safety:** Automatic handle cleanup, `IDisposable` implementation, administrator privilege checking[cite: 10].
- **Properties:** `ProcessHandle`, `ModuleBase`, `ModuleSize`, `Is64Bit`, `External`, `Internal`, `PatternScanner`[cite: 10].
- **Reading:** `ReadByte`, `ReadBool`, `ReadInt16`, `ReadUInt16`, `ReadInt32`, `ReadInt`, `ReadUInt32`, `ReadInt64`, `ReadUInt64`, `ReadFloat`, `ReadDouble`, `ReadMemory`, `ReadString`, `ReadPointer`, `ReadPointerChain`[cite: 10].
- **Writing:** `WriteByte`, `WriteBool`, `WriteInt16`, `WriteUInt16`, `WriteInt32`, `WriteInt`, `WriteUInt32`, `WriteInt64`, `WriteUInt64`, `WriteFloat`, `WriteDouble`, `WriteString`[cite: 10].
- **Generics:** `Read<T>()` / `Write<T>()` for unmanaged structs (e.g., `Vector3`)[cite: 10].
- **Patching & Utilities:** `Nop(address, length)`, `Patch(address, bytes)`, `GetModuleInfo(moduleName)`[cite: 10].
- **Memory Freezing:** Thread-safe constant memory writing via `FreezeMemory`, `FreezeInt`, `FreezeFloat`, `FreezeBool`, and `UnfreezeMemory`[cite: 10].

<details>
<summary><b>📁 Core Module (<code>MemoryEngine.Core</code>)</b></summary>

- **OffsetManager.cs**[cite: 2]
  - **Enums & Data Types:**
    - `OffsetType`: `Absolute`, `ModuleRelative`, `PointerChain`, `Pattern`, `DynamicExpression`, `InstructionAuto` (smart x86/x64 mode), `InstructionAbsolute` (direct 32-bit pointer), `InstructionRelative` (RIP-relative x64)[cite: 2].
    - `GameOffset`: Class containing `Name`, `Group`, `Type`, `ModuleName`, `Address`, `RawAddress`, `PointerChain`, `Pattern`, `Mask`, `PatternOffset`, `InstructionLength`, `ParentOffsetName`, `StructOffset`, and `DynamicResolver`[cite: 2].
    - `OffsetResolvedEventArgs`: Contains `Name` and `Address`[cite: 2].
  - **Methods & Events:**
    - Event: `OnOffsetResolved`[cite: 2].
    - Registration: `Add`, `AddAbsolute`, `AddModuleRelative` (supports Hex/Decimal strings), `AddCustom`, `AddPointerChain`, `AddPattern`, `AddInstructionAuto`, `AddInstructionRelative`, `AddInstructionAbsolute`, `AddStructMember`, `AddDynamic`[cite: 2].
    - Lookup & Grouping: `Remove`, `ResolveGroup`, `ValidateOffsets`, `GetNamesByGroup`, `Get`, `TryGetResolved`, `Contains`, `InvalidateCache`[cite: 2].
    - Resolution: `Resolve`, `ResolveAllAsync` (parallel background resolution with progress reporting)[cite: 2].
    - Serialization: `ExportToJson`, `SaveToFile`, `LoadFromJson`, `LoadFromFile`[cite: 2].
- **PatternScanner.cs**[cite: 3]
  - Signature scanning with wildcard support (`?` / `??`)[cite: 3].
  - Methods: `FindPattern(moduleName, patternString)`, `FindPattern(baseAddress, regionSize, patternString)`, `FindPattern(regionSize, patternString)`[cite: 3].
- **CacheManager.cs**[cite: 4]
  - Thread-safe memory caching layer based on `ConcurrentDictionary` and `TickCount64`[cite: 4].
  - Methods: `GetOrUpdate<T>(key, updateFunc, overrideExpirationMs)`, `Invalidate(key)`[cite: 4].
- **InputSystem.cs**[cite: 7]
  - Native key state queries via `user32.dll` `GetAsyncKeyState`[cite: 7].
  - Methods: `IsKeyDown(virtualKeyCode)`, `IsMenuToggled(menuKeyCode)`[cite: 7].
- **MemoryAccess.cs**[cite: 1]
  - Win32 API P/Invoke imports (`OpenProcess`, `ReadProcessMemory`, `WriteProcessMemory`, `VirtualAllocEx`, `VirtualProtectEx`, `VirtualFreeEx`, `CloseHandle`)[cite: 1].
  - Win32 Constants: `PROCESS_ALL_ACCESS`, `MEM_COMMIT`, `MEM_RESERVE`, `PAGE_EXECUTE_READWRITE`, `MEM_RELEASE`, `PAGE_READWRITE`[cite: 1].
- **HookInfo.cs**[cite: 6]
  - Class holding detour state: `HookAddress`, `CaveAddress`, `OriginalBytes`[cite: 6].
</details>

<details>
<summary><b>📁 External Module (<code>MemoryEngine.External</code>)</b></summary>

- **ExternalHooking.cs & StreamCodeWriter.cs**[cite: 8]
  - Out-of-process assembly injection and hooking using the **Iced** x86/x64 assembler[cite: 8].
  - **Memory & Pointers:** `Dereference(baseAddress, offsets)`[cite: 8].
  - **String Manipulation:** `ReadString(address, maxLength, encoding)`, `WriteString(address, text, encoding)`[cite: 8].
  - **NOPing & Patching:** `Nop(patchName, address, length)`, `ApplyPatch(patchName, address, newBytes)`, `RestorePatch(patchName)`[cite: 8].
  - **Detours (Iced):** `ApplyDetour`, `ApplyAobDetour`[cite: 8].
  - **Shellcode Execution:** `ExecuteShellcode(buildCode)` (executes dynamic assembly instructions in remote thread)[cite: 8].
  - **Symbol & Memory Management:** `GetOrCreateCave`, `RegisterSymbol`, `GetSymbolAddress`, `AllocateVariable<T>`, `InjectJMP`, `ReturnEarly`, `RemoveHook`, `RemoveAll`, `WriteMemory`, `FreeCave`[cite: 8].
</details>

<details>
<summary><b>📁 Internal Module (<code>MemoryEngine.Internal</code>)</b></summary>

- **InternalHooking.cs & SharedMemoryBridge.cs**[cite: 9]
  - In-process internal hooking using **MinHook** and **Iced**[cite: 9].
  - **UnsafeMemory Class:** Fast C++ style memory pointers: `UnsafeMemory.Read<T>`, `UnsafeMemory.Write<T>`, `UnsafeMemory.ReadPointer`[cite: 9].
  - **Enums & Structs:** `MemoryProtection`, `AllocationType`, `FreeType`, `SharedData` (`Flag`, `Entity`)[cite: 9].
  - **SharedMemoryBridge Class:** Allocates executable shared memory for IPC/Event transfer (`Address`, `Read()`, `ResetFlag()`, `Dispose()`)[cite: 9].
  - **InternalHooking Class:** `ApplyStandardHook<T>`, `ApplyCodeCave`, `ApplyEventCave`, `Dispose()`[cite: 9].
</details>

<details>
<summary><b>📁 Injector Module (<code>Injector</code>)</b></summary>

- **DllInjector.cs**[cite: 5]
  - Native DLL injection via `VirtualAllocEx`, `WriteProcessMemory`, `GetProcAddress("LoadLibraryA")`, and `CreateRemoteThread`[cite: 5].
  - Method: `DllInjector.Inject(processName, dllPath)`[cite: 5].
</details>

<details>
<summary><b>📁 Game Module (<code>MemoryEngine.Game</code> & <code>MemoryEngine</code>)</b></summary>

- **ViewMatrix.cs**[cite: 14]
  - `MatrixSettings`: Projection parameters (`IsColumnMajor`, `IsZeroToOneRange`, `InvertY`)[cite: 14].
  - `ViewMatrix`: Unsafe fixed array `float m[16]` matrix structure with GC-free `WorldToScreen(pos, out screenPos, width, height)` transformation[cite: 14].
- **EntityManager.cs (`Entity`)**[cite: 12]
  - Entity model (`Address`, `Health`, `X`, `Y`, `Z`, `Position`, `Team`, `Name`, `Pitch`, `Yaw`, `IsAlive`)[cite: 12].
  - Dynamic extensions: `CustomData` dictionary, `GetCustom<T>(key, defaultValue)`, `SetCustom<T>(key, value)`[cite: 12].
- **AimbotMath.cs**[cite: 15]
  - Aim angle calculation: `CalculateAngle(localPos, targetPos, entityKey, prediction, zOffset)` with frame velocity target prediction and `ClearHistory()`[cite: 15].
- **GameFunctionCaller.cs**[cite: 13]
  - `CallVoidFunction(functionAddress)` via remote thread creation[cite: 13].
</details>

<details>
<summary><b>📁 Overlay Module (<code>MemoryEngine.Overlay</code>)</b></summary>

- **ESP.cs (`ESP`, `EnemyScreenData`, `EspData`)**[cite: 11]
  - Hardware-accelerated, transparent, click-through WinForms overlay (`WS_EX_TRANSPARENT` / `WS_EX_LAYERED`)[cite: 11].
  - Background STA thread launcher: `ESP.Launch()`[cite: 11].
  - Thread-safe rendering synchronization via `EspData.LockObject` and `EspData.Enemies`[cite: 11].
  - `EnemyScreenData`: `HeadPos`, `FeetPos`, `Distance`, `BoxColor`, `CustomText`[cite: 11].
</details>

---

## 🚀 Usage Examples

### 1. Engine Initialization, Memory Freezing & Caching

```csharp
using System;
using MemoryEngine;
using MemoryEngine.Core;

// Setup OffsetManager with fluent registration
var offsets = new OffsetManager()
    .AddPointerChain("Health", "ac_client.exe", "0x109B74", new[] { 0xF8 })
    .AddModuleRelative("LocalPlayer", "ac_client.exe", "0x109B74")
    .AddInstructionAuto("AmmoInst", "ac_client.exe", "FF 0E 5F 5E 8B E5 5D C3", patternOffset: 0, instructionLength: 2);

// Attach Engine
using var engine = new Engine("ac_client");

// Resolve target addresses
IntPtr healthAddr = offsets.Resolve("Health", engine);

// Read, write & freeze memory
int currentHealth = engine.ReadInt(healthAddr);
engine.WriteInt(healthAddr, 100);
engine.FreezeInt(healthAddr, 100);

// Use CacheManager for high-frequency loop performance (~60 updates/sec)
var cache = new CacheManager(defaultExpirationMs: 16);
int cachedHealth = cache.GetOrUpdate("player_hp", () => engine.ReadInt(healthAddr));
```

### 2. Aimbot Angle Calculation & WorldToScreen Projection

```csharp
using System.Numerics;
using MemoryEngine.Game;

// Setup ViewMatrix
var matrix = new ViewMatrix
{
    Settings = new MatrixSettings { IsColumnMajor = true, InvertY = true }
};

Vector3 localPlayerPos = new Vector3(10f, 15f, 2f);
Vector3 enemyPos = new Vector3(25f, 30f, 2f);

// Calculate Aimbot Pitch & Yaw with movement prediction
var (pitch, yaw) = AimbotMath.CalculateAngle(localPlayerPos, enemyPos, entityKey: (IntPtr)0x12345, prediction: true);

// Project 3D World Position to 2D Screen Coordinates
if (matrix.WorldToScreen(enemyPos, out Vector2 screenPos, width: 1920, height: 1080))
{
    Console.WriteLine($"Enemy Screen X: {screenPos.X}, Y: {screenPos.Y}");
}
```

### 3. External Shellcode Execution & Code Caves (Iced)

```csharp
using MemoryEngine;
using static Iced.Intel.AssemblerRegisters;

using var engine = new Engine("ac_client");

// Allocate variable inside target process
IntPtr customVar = engine.External.AllocateVariable("MyCounter", 1337);

// Execute custom assembly remotely
engine.External.ExecuteShellcode(asm =>
{
    asm.mov(eax, 100);
    asm.add(eax, 200);
});
```

### 4. Input System & DLL Injection

```csharp
using Injector;
using MemoryEngine.Core;

// Check hotkeys (e.g., Insert key for menu toggling)
if (InputSystem.IsMenuToggled(0x2D)) // 0x2D = VK_INSERT
{
    Console.WriteLine("Menu Toggled!");
}

// Inject internal module into target process
bool injected = DllInjector.Inject("ac_client", @"C:\Hacks\InternalPayload.dll");
```

---

## ⚠️ Disclaimer & Limitation of Liability

**Educational Purposes Only:** This software ("MemoryEngine") is provided solely for educational, research, and reverse-engineering purposes, as well as for single-player application modifications.

**No Liability:** In no event shall the author(s) or copyright holder(s) be liable for any claim, damages, or other liability—whether in an action of contract, tort, or otherwise—arising from, out of, or in connection with the software or the use or other dealings in the software.

**Risk of Misuse & Account Bans:** The user assumes full responsibility for any actions taken with this library. The author does not endorse, encourage, or condone the use of this software in online competitive environments, anti-cheat protected software, or any commercial products. The author is strictly not responsible for any account bans, hardware ID bans, financial losses, or software instability resulting from the use or misuse of this code.

---

## 📜 License

Licensed under the [MIT License](LICENSE).