# MemoryEngine 🚀

[![NuGet Version](https://img.shields.io/nuget/v/MemoryEngine.svg)](https://www.nuget.org/packages/MemoryEngine)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A high-performance, modular **Game Hacking & Memory Manipulation Framework** written in C# (.NET 6.0+). Supports internal & external tooling, thread-safe offset management, pattern scanning, ICED detours, MinHook detours, event caves, and Windows Forms ESP overlays.

---

## 🛠 Features & API Surface

### Engine (`Engine.cs`)
- **Process Connection & Safety:** Automatic handle cleanup, `IDisposable` implementation, administrator privilege checking.
- **Reading:** `ReadByte`, `ReadBool`, `ReadInt16`, `ReadUInt16`, `ReadInt32`, `ReadInt`, `ReadUInt32`, `ReadInt64`, `ReadUInt64`, `ReadFloat`, `ReadDouble`, `ReadString`, `ReadPointer`, `ReadPointerChain`
- **Writing:** `WriteByte`, `WriteBool`, `WriteInt16`, `WriteUInt16`, `WriteInt32`, `WriteInt`, `WriteUInt32`, `WriteInt64`, `WriteUInt64`, `WriteFloat`, `WriteDouble`, `WriteString`
- **Generics:** `Read<T>()` / `Write<T>()` for unmanaged structs (e.g., `Vector3`).
- **Patching & Utilities:** `Nop(address, length)`, `Patch(address, bytes)`, `GetModuleInfo(moduleName)`
- **Freezing:** Thread-safe constant memory writing via `FreezeMemory`, `FreezeInt`, `FreezeFloat`, `FreezeBool`, and `UnfreezeMemory`.

<details>
<summary><b>📁 Core Module</b></summary>

- **OffsetManager.cs**
  - Thread-safe (`ConcurrentDictionary`) offset registry with runtime caching and JSON config support.
  - Supports `Absolute`, `ModuleRelative`, `PointerChain`, `Pattern`, `InstructionRelative` (RIP-relative x64), and `DynamicExpression` (Lambda) types.
  - Methods: `AddAbsolute`, `AddModuleRelative` (supports Hex strings), `AddPointerChain`, `AddPattern`, `AddInstructionRelative`, `AddStructMember`, `AddDynamic`, `Resolve`, `ResolveAllAsync`, `ExportToJson`, `LoadFromFile`.
- **PatternScanner.cs**
  - High-performance AOB signature scanning with wildcard mask support (`?` / `??`).
  - Methods: `FindPattern(moduleName, pattern)`, `FindPattern(baseAddress, regionSize, pattern)`.
- **MemoryAccess.cs**
  - Native Win32 P/Invoke wrappers (`OpenProcess`, `ReadProcessMemory`, `WriteProcessMemory`, `VirtualAllocEx`, `VirtualProtectEx`, `VirtualFreeEx`, `CloseHandle`).
- **HookInfo.cs**
  - Data container for active detours (`HookAddress`, `CaveAddress`, `OriginalBytes`).
</details>

<details>
<summary><b>📁 External Module</b></summary>

- **ExternalHooking.cs**
  - Out-of-process assembly detours and code caves using the **Iced** x86/x64 assembler.
  - Methods: `ApplyDetour`, `ApplyAobDetour`, `GetOrCreateCave`, `RegisterSymbol`, `AllocateVariable<T>`, `InjectJMP`, `ReturnEarly`, `RemoveHook`, `RemoveAll`.
</details>

<details>
<summary><b>📁 Internal Module</b></summary>

- **InternalHooking.cs & SharedMemoryBridge.cs**
  - In-process hook management using **MinHook** and **Iced**.
  - Background event loop cave execution via shared memory bridges.
  - Methods: `ApplyStandardHook<T>`, `ApplyCodeCave`, `ApplyEventCave`, `SharedMemoryBridge.Read()`, `SharedMemoryBridge.ResetFlag()`.
</details>

<details>
<summary><b>📁 Game Module</b></summary>

- **ViewMatrix.cs**
  - Allocation-free 4x4 matrix projection for 3D World to 2D Screen transformations (`WorldToScreen`).
  - Supports Column-Major layout, Depth range adjustment, and Y-axis inversion via `MatrixSettings`.
- **EntityManager.cs (`Entity`)**
  - Universal entity data model with position vector, health, pitch/yaw, and dynamic `CustomData` dictionary extensions.
- **AimbotMath.cs**
  - Calculates Pitch & Yaw angles to target entities. Includes frame-velocity target prediction (`CalculateAngle`, `ClearHistory`).
- **GameFunctionCaller.cs**
  - Remote function execution via thread creation (`CallVoidFunction`).
</details>

<details>
<summary><b>📁 Overlay Module</b></summary>

- **ESP.cs (ESPOverlay)**
  - Hardware-accelerated, click-through, transparent Windows Forms overlay running on a background STA thread.
  - Thread-safe data transfer via `EspData.LockObject` and `EspData.Enemies`.
  - Configurable rendering via `EnemyScreenData` (`HeadPos`, `FeetPos`, `Distance`, `BoxColor`, `CustomText`).
</details>

---

## 🚀 Usage Examples

### Initialization & Dynamic Offset Resolution

```csharp
using System;
using MemoryEngine;
using MemoryEngine.Core;

// 1. Setup OffsetManager with fluent registration
var offsets = new OffsetManager()
    .AddPointerChain("Health", "ac_client.exe", "0x109B74", new[] { 0xF8 })
    .AddModuleRelative("LocalPlayer", "ac_client.exe", "0x109B74")
    .AddPattern("AmmoPattern", "ac_client.exe", "FF 0E 5F 5E 8B E5 5D C3");

// 2. Attach Engine
using var engine = new Engine("ac_client");

// 3. Resolve runtime target addresses
IntPtr healthAddr = offsets.Resolve("Health", engine);

// 4. Read & Write
int currentHealth = engine.ReadInt(healthAddr);
engine.WriteInt(healthAddr, 100);

// 5. Freeze memory value
engine.FreezeInt(healthAddr, 100);
```

### Hardware Overlay Rendering (ESP)

```csharp
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using MemoryEngine.Overlay;

// Launch transparent overlay thread
ESP.Launch();

// Inside your main game loop: Update rendered frames
var frameData = new List<EnemyScreenData>
{
    new EnemyScreenData
    {
        HeadPos = new Vector2(960, 300),
        FeetPos = new Vector2(960, 500),
        Distance = 12.5f,
        BoxColor = Color.Red,
        CustomText = "Enemy [100 HP]"
    }
};

lock (EspData.LockObject)
{
    EspData.Enemies = frameData;
}
```

---

## ⚠️ Disclaimer & Limitation of Liability

**Educational Purposes Only:** This software ("MemoryEngine") is provided solely for educational, research, and reverse-engineering purposes, as well as for single-player application modifications.

**No Liability:** In no event shall the author(s) or copyright holder(s) be liable for any claim, damages, or other liability—whether in an action of contract, tort, or otherwise—arising from, out of, or in connection with the software or the use or other dealings in the software.

**Risk of Misuse & Account Bans:** The user assumes full responsibility for any actions taken with this library. The author does not endorse, encourage, or condone the use of this software in online competitive environments, anti-cheat protected software, or any commercial products. The author is strictly not responsible for any account bans, hardware ID bans, financial losses, or software instability resulting from the use or misuse of this code.

---

## 📜 License

Licensed under the [MIT License](LICENSE).