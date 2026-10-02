# MemoryEngine 🚀

[![NuGet Version](https://img.shields.io/nuget/v/MemoryEngine.svg)](https://www.nuget.org/packages/MemoryEngine)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A high-performance, modular **Game Hacking & Memory Manipulation Framework** written in C# (.NET 6.0+). Supports internal & external tooling, pattern scanning, ICED detours, MinHook detours, event caves, and Windows Forms ESP overlays.

---

## 🛠 Features & API Surface

* **Engine.cs**
  <ul>
    <li><code>GetModuleInfo(moduleName)</code></li>
    <li><code>ReadByte</code> / <code>ReadBool</code> / <code>ReadInt16</code> / <code>ReadUInt16</code> / <code>ReadInt32</code> / <code>ReadInt</code> / <code>ReadUInt32</code> / <code>ReadInt64</code> / <code>ReadUInt64</code> / <code>ReadFloat</code> / <code>ReadDouble</code> / <code>ReadString</code></li>
    <li><code>WriteByte</code> / <code>WriteBool</code> / <code>WriteInt16</code> / <code>WriteUInt16</code> / <code>WriteInt32</code> / <code>WriteInt</code> / <code>WriteUInt32</code> / <code>WriteInt64</code> / <code>WriteUInt64</code> / <code>WriteFloat</code> / <code>WriteDouble</code> / <code>WriteString</code></li>
    <li><code>Read&lt;T&gt;()</code> / <code>Write&lt;T&gt;()</code> (Generisch für Unmanaged Structs)</li>
    <li><code>ReadPointer(address)</code> / <code>ReadPointerChain(baseAddress, offsets)</code></li>
    <li><code>Nop(address, length)</code> / <code>Patch(address, bytes)</code></li>
    <li><code>FreezeMemory</code> / <code>FreezeInt</code> / <code>FreezeFloat</code> / <code>FreezeBool</code> / <code>UnfreezeMemory</code></li>
  </ul>

<details>
  <summary><b>📁 Core</b></summary>
  <ul>
    <li><b>HookInfo.cs</b>
      <ul>
        <li><code>HookAddress</code> (IntPtr)</li>
        <li><code>CaveAddress</code> (IntPtr)</li>
        <li><code>OriginalBytes</code> (byte[]?)</li>
      </ul>
    </li>
    <li><b>MemoryAccess.cs</b>
      <ul>
        <li><code>OpenProcess</code></li>
        <li><code>ReadProcessMemory</code> / <code>WriteProcessMemory</code></li>
        <li><code>VirtualAllocEx</code> / <code>VirtualProtectEx</code> / <code>VirtualFreeEx</code> / <code>CloseHandle</code></li>
      </ul>
    </li>
    <li><b>OffsetManager.cs</b>
      <ul>
        <li><code>AddAbsolute(name, address)</code></li>
        <li><code>AddModuleRelative(name, moduleName, offset)</code></li>
        <li><code>AddPointerChain(name, moduleName, baseOffset, chain)</code></li>
        <li><code>AddPattern(name, moduleName, pattern)</code></li>
        <li><code>Resolve(name, engine)</code></li>
        <li><code>Get(name)</code> / <code>Contains(name)</code></li>
      </ul>
    </li>
    <li><b>PatternScanner.cs</b>
      <ul>
        <li><code>FindPattern(moduleName, patternString)</code></li>
        <li><code>FindPattern(baseAddress, regionSize, patternString)</code></li>
        <li><code>FindPattern(regionSize, patternString)</code></li>
      </ul>
    </li>
  </ul>
</details>

<details>
  <summary><b>📁 External</b></summary>
  <ul>
    <li><b>ExternalHooking.cs</b>
      <ul>
        <li><code>ApplyDetour(featureName, hookAddress, instructionLength, buildCaveCode)</code></li>
        <li><code>ApplyAobDetour(featureName, moduleName, pattern, instructionLength, buildCaveCode)</code></li>
        <li><code>GetOrCreateCave(name, size)</code> / <code>FreeCave(featureName)</code></li>
        <li><code>RegisterSymbol(name, address)</code> / <code>GetSymbolAddress(name)</code></li>
        <li><code>AllocateVariable&lt;T&gt;(symbolName, initialValue)</code></li>
        <li><code>InjectJMP(source, destination, totalLengthToNop)</code></li>
        <li><code>ReturnEarly(address)</code></li>
        <li><code>WriteMemory(address, data)</code></li>
        <li><code>RemoveHook(featureName)</code> / <code>RemoveAll()</code></li>
      </ul>
    </li>
  </ul>
</details>

<details>
  <summary><b>📁 Game</b></summary>
  <ul>
    <li><b>EntityManager.cs</b>
      <ul>
        <li><code>Entity</code> (Address, Health, X, Y, Z)</li>
      </ul>
    </li>
    <li><b>GameFunctionCaller.cs</b>
      <ul>
        <li><code>CallVoidFunction(functionAddress)</code></li>
      </ul>
    </li>
    <li><b>ViewMatrix.cs</b>
      <ul>
        <li><code>WorldToScreen(pos, out screenPos, width, height)</code></li>
        <li><code>MatrixSettings</code> (IsColumnMajor, IsZeroToOneRange, InvertY)</li>
      </ul>
    </li>
  </ul>
</details>

<details>
  <summary><b>📁 Internal</b></summary>
  <ul>
    <li><b>InternalHooking.cs</b>
      <ul>
        <li><code>ApplyStandardHook&lt;T&gt;(targetAddress, detourDelegate)</code></li>
        <li><code>ApplyCodeCave(caveName, hookAddress, bytesToOverwrite, buildCaveCode)</code></li>
        <li><code>ApplyEventCave(name, hookAddress, bytesToOverwrite, buildAsm, onTrigger)</code></li>
        <li><code>SharedMemoryBridge</code> (Read, ResetFlag)</li>
      </ul>
    </li>
  </ul>
</details>

<details>
  <summary><b>📁 Overlay</b></summary>
  <ul>
    <li><b>ESP.cs</b>
      <ul>
        <li><code>ESP.Launch()</code></li>
        <li><code>EnemyScreenData</code> (HeadPos, FeetPos, Distance, BoxColor, CustomText)</li>
        <li><code>EspData</code> (LockObject, Enemies)</li>
      </ul>
    </li>
  </ul>
</details>

---

## 🚀 Usage Examples

--------------------------------   Initialization & Offset Resolution   --------------------------------
```csharp
using MemoryEngine;
using MemoryEngine.Core;

// 1. Setup OffsetManager
var offsets = new OffsetManager();
offsets.AddPointerChain("Health", "ac_client.exe", (IntPtr)0x109B74, new int[] { 0xF8 });
offsets.AddModuleRelative("LocalPlayer", "ac_client.exe", (IntPtr)0x109B74);

// 2. Attach Engine
using var engine = new Engine("ac_client");

// 3. Resolve & Read
IntPtr healthAddr = offsets.Resolve("Health", engine);
int health = engine.ReadInt(healthAddr);

// Write back
engine.WriteInt(healthAddr, 100);
```

--------------------------------   ESP   --------------------------------
```csharp
using System.Drawing;
using System.Numerics;
using MemoryEngine.Overlay;

// Launch Hardware Overlay Thread
ESP.Launch();

// Game Loop: Push rendered frames
var list = new List<EnemyScreenData>
{
    new EnemyScreenData
    {
        HeadPos = new Vector2(960, 300),
        FeetPos = new Vector2(960, 500),
        Distance = 12.5f,
        BoxColor = Color.Red,
        CustomText = "Enemy"
    }
};

lock (EspData.LockObject)
{
    EspData.Enemies = list;
}
```




## ⚠️ Disclaimer & Limitation of Liability

**Educational Purposes Only:** This software ("MemoryEngine") is provided solely for educational, research, and reverse-engineering purposes, as well as for single-player application modifications.

**No Liability:** In no event shall the author(s) or copyright holder(s) be liable for any claim, damages, or other liability—whether in an action of contract, tort, or otherwise—arising from, out of, or in connection with the software or the use or other dealings in the software.

**Risk of Misuse & Account Bans:** The user assumes full responsibility for any actions taken with this library. The author does not endorse, encourage, or condone the use of this software in online competitive environments, anti-cheat protected software, or any commercial products. The author is strictly not responsible for any account bans, hardware ID bans, financial losses, or software instability resulting from the use or misuse of this code.

## 📜 License
Licensed under the MIT License.