
# ==================================================
Created with AI, so please verify all information is correct and up-to-date. This documentation is intended to be beginner-friendly with this package, but it is not a substitute for understanding C#, Windows memory management, or game hacking concepts.
# MemoryEngine - Ultimate Beginner-to-Advanced Documentation
Welcome to MemoryEngine! This document is designed so that anyone—from absolute beginners to experienced developers—can easily understand and use every single class, property, and method in this framework.
Aimbot and other advanced features are not included in this documentation, but the framework provides all the necessary tools

---

## Table of Contents

1. Absolute Beginner's Guide (Start Here!)
2. Universal Root (Engine.cs)
3. Core Module
* OffsetManager.cs
* PatternScanner.cs
* MemoryAccess.cs
* HookInfo.cs


4. External Module
* ExternalHooking.cs


5. Internal Module
* InternalHooking.cs
* SharedMemoryBridge.cs


6. Game Module
* ViewMatrix.cs
* EntityManager.cs
* GameFunctionCaller.cs


7. Overlay Module
* ESP.cs (ESPOverlay, EnemyScreenData, EspData)



---

1. Absolute Beginner's Guide

---

What is MemoryEngine?
MemoryEngine is a tool that allows your C# application to read, modify, and render custom visual overlays (like ESP / Wallhacks) over running computer games.

Key Concepts in Plain English:

* Process Name: The executable name of the game (e.g., "ac_client" for AssaultCube).
* Memory Address (IntPtr): A specific location inside the computer's RAM where game information (like Health, Ammo, or Coordinates) is saved.
* Offsets: Instructions on how to calculate a memory location, even if the game restarts and memory locations change.
* Freeze: Automatically re-writing a value thousands of times a second so the game cannot decrease your Health or Ammo.
* Overlay: A transparent window placed over your game that draws visual boxes or distance indicators.

How to start any basic project:
Step 1: Open your C# program.
Step 2: Connect MemoryEngine to your game using:
var engine = new Engine("ac_client");
Step 3: Read or write memory addresses!

---

2. Universal Root (Engine.cs)

---

The Engine class is the core component of the framework. It handles process connections, reading/writing memory, memory patching, and memory freezing.

Class Properties:

* ProcessHandle (IntPtr): The open Windows system handle used to talk to the game process.
* ModuleBase (IntPtr): The starting memory address of the game's main EXE file.
* ModuleSize (int): Total size in bytes of the game's main EXE in memory.
* Is64Bit (bool): Returns True if the target game is a 64-bit application, or False if it is 32-bit.
* External (ExternalHooking): Access point for external assembly code injection and code caves.
* Internal (InternalHooking): Access point for internal DLL hooks and event caves.
* PatternScanner (PatternScanner): Access point for finding AOB memory signatures.

Constructor:

* public Engine(string processName, bool force32Bit = false)
Connects to the specified running game process.
Example usage:
var engine = new Engine("ac_client");

Process & Module Methods:

* public (IntPtr BaseAddress, int ModuleSize) GetModuleInfo(string moduleName)
Finds the base memory address and memory size of any loaded DLL or EXE (e.g., "client.dll").

Reading Methods (Getting values from the game):

* public byte ReadByte(IntPtr address)
Reads a single byte (0 to 255).
* public bool ReadBool(IntPtr address)
Reads a true/false condition (0 = False, Non-zero = True).
* public short ReadInt16(IntPtr address)
Reads a 2-byte signed number (-32,768 to 32,767).
* public ushort ReadUInt16(IntPtr address)
Reads a 2-byte positive number (0 to 65,535).
* public int ReadInt32(IntPtr address)
Reads a standard 4-byte integer number.
* public int ReadInt(IntPtr address)
Alias helper method for ReadInt32.
* public uint ReadUInt32(IntPtr address)
Reads a positive 4-byte integer.
* public long ReadInt64(IntPtr address)
Reads an 8-byte integer (used for 64-bit pointers).
* public ulong ReadUInt64(IntPtr address)
Reads an unsigned 8-byte integer.
* public float ReadFloat(IntPtr address)
Reads a decimal number (used for X, Y, Z coordinates and camera rotations).
* public double ReadDouble(IntPtr address)
Reads a high-precision decimal number.
* public byte[] ReadMemory(IntPtr address, int size)
Reads a raw block of bytes of any specified length.
* public string ReadString(IntPtr address, int length, Encoding? encoding = null)
Reads text (such as player names) up to the specified character length.
* public IntPtr ReadPointer(IntPtr address)
Reads a memory address pointing to another location in memory.
* public IntPtr ReadPointerChain(IntPtr baseAddress, int[] offsets)
Follows a path of multiple pointer offsets to reach a target variable.
Example usage:
IntPtr healthAddr = engine.ReadPointerChain(baseModule, new int[] { 0x10, 0x4, 0xF8 });
* public T Read(IntPtr address) where T : unmanaged
Reads an entire C# struct (like a Vector3 position or custom Player Data struct) at once.

Writing Methods (Changing values in the game):

* public void WriteByte(IntPtr address, byte value)
* public void WriteBool(IntPtr address, bool value)
* public void WriteInt16(IntPtr address, short value)
* public void WriteUInt16(IntPtr address, ushort value)
* public void WriteInt32(IntPtr address, int value)
* public void WriteInt(IntPtr address, int value)
* public void WriteUInt32(IntPtr address, uint value)
* public void WriteInt64(IntPtr address, long value)
* public void WriteUInt64(IntPtr address, ulong value)
* public void WriteFloat(IntPtr address, float value)
* public void WriteDouble(IntPtr address, double value)
* public void WriteString(IntPtr address, string text, Encoding? encoding = null)
* public void Write(IntPtr address, T value) where T : unmanaged
All write methods overwrite memory at the specified address with your desired value.

Utility Methods:

* public void Nop(IntPtr address, int length)
Replaces game instructions with NOP (0x90) bytes, effectively disabling game functions (e.g., disabling recoil or damage routines).
* public void Patch(IntPtr address, byte[] bytes)
Overwrites specific game assembly bytes with custom bytes.

Freezing Methods (Cheat Engine Style Constant Writing):

* public void FreezeMemory(IntPtr address, byte[] value)
* public void FreezeInt(IntPtr address, int value)
* public void FreezeFloat(IntPtr address, float value)
* public void FreezeBool(IntPtr address, bool value)
Continuously enforces your chosen value on a background thread every 10 milliseconds.
* public void UnfreezeMemory(IntPtr address)
Stops enforcing constant values for the target memory location.

Cleanup Method:

* public void Dispose()
Safely closes all open process handles and clears background threads.

---

3. Core Module

---

OffsetManager.cs & GameOffset.cs
Easily manage and resolve game memory locations regardless of game updates or ASLR (Address Space Layout Randomization).

Offset Types Supported:

1. Absolute: Fixed memory address.
2. ModuleRelative: Module Base Address + Offset.
3. PointerChain: Module Base Address + Pointer Path.
4. Pattern: AOB Signature Scan performed at runtime.

Methods:

* public void AddAbsolute(string name, IntPtr address)
Registers a static address.
* public void AddModuleRelative(string name, string moduleName, IntPtr offset)
Registers a module-relative address.
* public void AddPointerChain(string name, string moduleName, IntPtr baseOffset, int[] chain)
Registers a pointer path.
* public void AddPattern(string name, string moduleName, string pattern)
Registers an AOB signature pattern.
* public GameOffset Get(string name)
Retrieves the stored offset configuration.
* public bool Contains(string name)
Checks if an offset name is registered.
* public IntPtr Resolve(string name, Engine engine)
Calculates the exact current memory location for any registered offset type.
Example usage:
IntPtr playerHealth = offsetManager.Resolve("Health", engine);

PatternScanner.cs
Scans the game's RAM for unique byte sequences (signatures).

Methods:

* public IntPtr FindPattern(string moduleName, string patternString)
Searches a specific module for an AOB pattern string (supports '??' wildcards).
Example pattern: "8B 15 ?? ?? ?? ?? 8B 34 88"
* public IntPtr FindPattern(IntPtr baseAddress, int regionSize, string patternString)
Searches a specific memory range for a pattern.
* public IntPtr FindPattern(int regionSize, string patternString)
Searches from the main module base for a pattern.

MemoryAccess.cs
Low-level Win32 P/Invoke declarations for kernel32.dll operations (OpenProcess, ReadProcessMemory, WriteProcessMemory, VirtualAllocEx, VirtualProtectEx, VirtualFreeEx, CloseHandle).

HookInfo.cs
Data container storing hook details:

* HookAddress (IntPtr): Location in memory where the hook was injected.
* CaveAddress (IntPtr): Location of allocated memory where custom assembly code runs.
* OriginalBytes (byte[]): Backup of original bytes for restoration when removing hooks.

---

4. External Module

---

ExternalHooking.cs
Enables external x86/x64 assembly detours and code caves without injecting a custom DLL into the game process. Uses the ICED assembly library.

Methods:

* public IntPtr GetOrCreateCave(string name, int size = 1024)
Allocates an executable memory region inside the target game process.
* public void ApplyDetour(string featureName, IntPtr hookAddress, int instructionLength, Action<Assembler, List> buildCaveCode)
Injects a JMP instruction at hookAddress directing execution flow to a custom ICED code cave.
* public IntPtr ApplyAobDetour(string featureName, string moduleName, string pattern, int instructionLength, Action<Assembler, List> buildCaveCode)
Locates an AOB pattern automatically and applies an ICED assembly detour.
* public void RegisterSymbol(string name, IntPtr address)
Stores a named memory address inside the symbol database.
* public IntPtr GetSymbolAddress(string name)
Retrieves a symbol's memory address.
* public IntPtr AllocateVariable(string symbolName, T initialValue = default) where T : unmanaged
Allocates a variable inside the game's memory space for assembly code interaction.
* public void InjectJMP(IntPtr source, IntPtr destination, int totalLengthToNop = 0)
Writes an absolute (x64) or relative (x86) jump instruction.
* public void ReturnEarly(IntPtr address)
Patches a game function to immediately return by writing a RET (0xC3) byte.
* public void RemoveHook(string featureName)
Restores original game instructions and frees the allocated code cave.
* public void FreeCave(string featureName)
Frees allocated memory for a specific code cave.
* public void RemoveAll()
Removes all active hooks, frees code caves, and clears stored symbols.

---

5. Internal Module

---

InternalHooking.cs
Used for in-process DLL injection projects requiring native MinHook detours or event-driven assembly code caves.

Methods:

* public T ApplyStandardHook(IntPtr targetAddress, T detourDelegate) where T : Delegate
Hooks a native game function using MinHook and redirects it to a C# delegate.
* public IntPtr ApplyCodeCave(string caveName, IntPtr hookAddress, int bytesToOverwrite, Action buildCaveCode)
Injects an in-process ICED code cave detour.
* public void ApplyEventCave(string name, IntPtr hookAddress, int bytesToOverwrite, Action<Assembler, uint> buildAsm, Action onTrigger)
Creates a code cave that signals a C# event callback whenever the game executes specific instructions.

SharedMemoryBridge.cs
Allocates shared executable memory structures for cross-thread event cave signaling.

* SharedData: Contains trigger flags and entity pointer addresses.
* Read(): Reads current shared bridge data.
* ResetFlag(): Resets the trigger flag after processing an event.

---

6. Game Module

---

ViewMatrix.cs
Represents a 4x4 matrix used to convert 3D World Coordinates (X, Y, Z) into 2D Screen Coordinates (X, Y) for ESP overlays.

Structs:

* MatrixSettings:
* IsColumnMajor (bool): Set True if the game uses Column-Major matrix layout.
* IsZeroToOneRange (bool): Set True if screen Depth ranges from 0 to 1 instead of -1 to 1.
* InvertY (bool): Set True if the Y axis needs to be inverted.



Methods:

* public bool WorldToScreen(Vector3 pos, out Vector2 screenPos, int width, int height)
Projects a 3D position into 2D screen coordinates. Returns False if the target is behind the player's camera.

EntityManager.cs
Simple data model representing a game entity:

* Address (IntPtr): Memory address of the entity.
* Health (int): Entity health value.
* X, Y, Z (float): 3D positional coordinates.

GameFunctionCaller.cs
Calls native game functions using remote process threads.

Methods:

* public GameFunctionCaller(Process process)
Initializes thread creation context for the game process.
* public bool CallVoidFunction(IntPtr functionAddress)
Creates a remote thread starting at the specified function address.

---

7. Overlay Module

---

ESP.cs (ESPOverlay)
Hardware-accelerated, transparent, click-through Windows Forms overlay that renders entity boxes and distance labels over games.

Methods:

* public static void Launch()
Launches the transparent overlay in its own background STA Thread with a single line of code.
Example usage:
ESP.Launch();

Data Structures for Displaying Enemies:

* EnemyScreenData:
* HeadPos (Vector2): 2D screen coordinate for the top of the entity box.
* FeetPos (Vector2): 2D screen coordinate for the bottom of the entity box.
* Distance (float): Distance to the target in meters/units.
* BoxColor (Color): Customizable bounding box color for each target.
* CustomText (string): Optional custom label (e.g. "PlayerName [100 HP]").


* EspData:
* LockObject (object): Object used for thread-safe lock synchronization.
* Enemies (List): Global list of screen data read by the overlay renderer.



Example of Updating Overlay Data inside your main loop:

var list = new List();

list.Add(new EnemyScreenData
{
HeadPos = new Vector2(960, 300),
FeetPos = new Vector2(960, 500),
Distance = 15.0f,
BoxColor = Color.Red,
CustomText = "Enemy"
});

lock (EspData.LockObject)
{
EspData.Enemies = list;
}

==================================================