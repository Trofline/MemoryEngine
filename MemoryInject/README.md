# MemoryEngine

`MemoryEngine` is a lightweight, external C# library built to simplify game memory manipulation and 3D-to-2D screen projection for ESP (Extra Sensory Perception) overlays. It is designed for educational purposes in the field of reverse engineering and game engine architecture.

## Features

* **External Memory Access:** Read integers, floats, and pointers from target process memory without the need for DLL injection.
* **Module Support:** Automatically calculate base addresses to handle dynamic memory allocation.
* **Universal ViewMatrix:** A highly configurable projection engine that translates 3D game coordinates to 2D screen pixels for any game engine.
* **Zero Dependencies:** Pure C# implementation using `System.Diagnostics` and `System.Numerics`.

---

## Core Classes

### 1. `Engine` Class
Handles the communication with the target process.
* `ReadMemory(IntPtr address, int size)`: Reads raw bytes from memory.
* `ReadPointer(IntPtr address)`: Follows a memory address to retrieve a base pointer.
* `ReadFloat(IntPtr address)` / `ReadInt(IntPtr address)`: Convenience wrappers for data types.

### 2. `ViewMatrix` Class
Handles the linear algebra required for ESP.
* `ViewMatrix.FromBytes(...)`: Converts raw memory bytes into a structured 4x4 matrix.
* `WorldToScreen(...)`: Projects 3D world coordinates into 2D overlay coordinates.

---

## Usage Example

```csharp
using MemoryEngine;
using System.Numerics;

// 1. Initialize the Engine
Engine engine = new Engine("game_process_name", force32Bit: true);

// 2. Configure Projection Settings (Universal across engines)
MatrixSettings settings = new MatrixSettings { 
    IsColumnMajor = false, 
    IsZeroToOneRange = false, 
    InvertY = true 
};

// 3. Main Loop Logic
void Update() 
{
    // Fetch Matrix and Entity List
    byte[] mBytes = engine.ReadMemory((IntPtr)0x501AE8, 64);
    ViewMatrix matrix = ViewMatrix.FromBytes(mBytes, settings);
    
    IntPtr entityList = engine.ReadPointer(engine.ModuleBase + 0x10F4F8);
    int numEntities = engine.ReadInt(engine.ModuleBase + 0x10F500);

    for (int i = 0; i < numEntities; i++) 
    {
        IntPtr entity = engine.ReadPointer(entityList + (i * 4));
        
        // Read coordinates
        float x = engine.ReadFloat(entity + 0x34);
        float y = engine.ReadFloat(entity + 0x38);
        float z = engine.ReadFloat(entity + 0x3C);

        // Project to screen
        if (matrix.WorldToScreen(new Vector3(x, y, z), out Vector2 screenPos, width, height)) 
        {
            // Draw your ESP element here
            DrawBox(screenPos);
        }
    }
}