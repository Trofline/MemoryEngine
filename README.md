# MemoryEngine

# This README is not up2date!!! There are many new features, Ive pasted an example in csharp code below...

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
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MemoryEngine;
using MemoryEngine.Core;
using MemoryEngine.External;
using static Iced.Intel.AssemblerRegisters;

namespace GTFO_CheatClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Engine engine;
        ExternalHooking hooker;
        PatternScanner patternScanner;

        IntPtr freezeAmmoAddr;
        bool freezeAmmoStatus = false;

        public MainWindow()
        {
            InitializeComponent(); //UI
            this.Closed += Window_Closed; //Event-Handler
        }

        private void FreezeAmmoBtn_Click(object sender, RoutedEventArgs e)
        {
            int stateToWrite = freezeAmmoStatus ? 0 : 1;
            engine.Write<int>(freezeAmmoAddr,stateToWrite);
            freezeAmmoStatus = !freezeAmmoStatus;


            //Optional
            FreezeAmmoBtn.Content = freezeAmmoStatus ? "Freeze: ON" : "Freeze: OFF";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            engine = new Engine("GTFO");
            hooker = new ExternalHooking(engine);
            patternScanner = new PatternScanner(engine);
            freezeAmmoAddr = hooker.AllocateVariable<int>("FreezeAmmoVariable",0);
            FreezeAmmoInit();
        }

        private void FreezeAmmoInit()
        {
            string aob = "FF 8B 90 02 00 00 48 8B 82 ?? ?? ?? ?? 48 8B 92 ?? ?? ?? ?? FF D0";
            int aobLength = 22;

            hooker.ApplyAobDetour("FreezeAmmo", "GameAssembly.dll", aob, aobLength,
                (asm, originalInstructions) =>
                {
                    var makeOriginal = asm.CreateLabel();

                    asm.mov(rax,freezeAmmoAddr.ToInt64());
                    asm.cmp(__dword_ptr[rax],1);
                    asm.je(makeOriginal);
                    //remove/skip dec
                    originalInstructions.RemoveAt(0);

                    asm.Label(ref makeOriginal);
                    //make everything normal again
                    asm.AddInstruction(originalInstructions[0]);
                });

//0. dec[rbx + 00000290]
//1. mov rax,[rdx + 00000AB0]
//2. mov rdx,[rdx + 00000AB8]
//3. call rax

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            hooker.RemoveAll();
            engine.Dispose();
            Environment.Exit(0);
        }
    }
}
