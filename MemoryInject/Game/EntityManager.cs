using System;
using System.Collections.Generic;
using System.Numerics;

namespace MemoryEngine.Game
{
    public class Entity
    {
        // --- Standard-Eigenschaften (wird in fast jedem Shooter gebraucht) ---
        public IntPtr Address { get; set; }
        public int Health { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public int Team { get; set; }
        public string Name { get; set; } = string.Empty;

        // Vektor-Position statt einzelner X/Y/Z-Floats (macht Mathe & Aimbot viel leichter!)
        public Vector3 Position { get; set; }

        // Blickwinkel (falls die Entity selbst schaut, z.B. für Spieler)
        public float Pitch { get; set; }
        public float Yaw { get; set; }

        // Hilfreiche Computed Properties
        public bool IsAlive => Health > 0;

        // ==========================================
        // 🔥 POWER-FEATURE: Eigene/Spielspezifische Daten
        // ==========================================
        // Hier kann der Nutzer beliebige Zusatzdaten speichern (z.B. Armor, Ammo, Score),
        // die das Framework standardmäßig nicht kennt.
        public Dictionary<string, object> CustomData { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        // Typsichere Helfer-Methoden für die Custom-Daten
        public T GetCustom<T>(string key, T defaultValue = default!)
        {
            if (CustomData.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        public void SetCustom<T>(string key, T value)
        {
            CustomData[key] = value!;
        }
    }
}