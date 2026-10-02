using System;
using System.Collections.Generic;
using System.Numerics;

namespace MemoryEngine.Game
{
    public static class AimbotMath
    {
        private static readonly Dictionary<IntPtr, Vector3> _lastTargetPositions = new Dictionary<IntPtr, Vector3>();

        public static (float pitch, float yaw) CalculateAngle(Vector3 localPos, Vector3 targetPos, IntPtr entityKey = default, bool prediction = false, float zOffset = 0.5f)
        {
            Vector3 finalTarget = targetPos;

            if (prediction && entityKey != default)
            {
                if (_lastTargetPositions.TryGetValue(entityKey, out Vector3 lastPos))
                {
                    // Berechne die echte Bewegung seit dem letzten Frame anhand der rohen Position
                    Vector3 velocity = targetPos - lastPos;
                    float predictionFactor = 1.2f; // Etwas sanfter starten (1.2 statt 1.5)
                    finalTarget += velocity * predictionFactor;
                }
                // Speichere die rohe (aktuelle) Position für den nächsten Frame
                _lastTargetPositions[entityKey] = targetPos;
            }

            // Erst jetzt den Kopf-Offset auf das vorausberechnete Ziel draufrechnen
            finalTarget.Z += zOffset;

            float deltaX = finalTarget.X - localPos.X;
            float deltaY = finalTarget.Y - localPos.Y;
            float deltaZ = finalTarget.Z - localPos.Z;

            // Korrigierte Zeile: Satz des Pythagoras für die 2D-Distanz
            float horizontalDistance = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);

            float yaw = MathF.Atan2(deltaY, deltaX) * (180f / MathF.PI) + 90f;
            float pitch = MathF.Atan2(deltaZ, horizontalDistance) * (180f / MathF.PI);

            return (pitch, yaw);
        }

        public static void ClearHistory()
        {
            _lastTargetPositions.Clear();
        }
    }
}