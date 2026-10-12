using System;
using System.Runtime.InteropServices;

namespace MemoryEngine.Core
{
    public static class InputSystem
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        // Prüft, ob die Taste in genau diesem Moment gedrückt wird
        public static bool IsKeyDown(int virtualKeyCode)
        {
            // Das höchste Bit (0x8000) gibt an, ob die Taste aktuell gedrückt ist.
            return (GetAsyncKeyState(virtualKeyCode) & 0x8000) != 0;
        }

        // Nützlich für Toggles (z.B. Menü auf/zu), damit ein Tastendruck nicht mehrfach registriert wird
        private static bool _wasMenuKeyPressed = false;

        public static bool IsMenuToggled(int menuKeyCode = 0x2D) // 0x2D = Insert/Einfügen
        {
            bool isPressed = IsKeyDown(menuKeyCode);

            if (isPressed && !_wasMenuKeyPressed)
            {
                _wasMenuKeyPressed = true;
                return true; // Toggle auslösen
            }
            else if (!isPressed)
            {
                _wasMenuKeyPressed = false;
            }

            return false;
        }
    }
}