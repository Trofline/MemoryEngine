using System;
using System.Numerics;

namespace MemoryEngine.Game
{
    public static class AimbotMath
    {
        // Beispiel: Ein universeller Ansatz, bei dem du die Skalierung und Offsets pro Spiel anpassen kannst
        public static (float pitch, float yaw) CalculateAngle(Vector3 localPos, Vector3 targetPos, float zOffset = 0.0f)
        {
            // Zielposition mit spielspezifischem Höhen-Offset (z.B. Kopf-Korrektur)
            Vector3 adjustedTarget = new Vector3(targetPos.X, targetPos.Y, targetPos.Z + zOffset);

            float deltaX = adjustedTarget.X - localPos.X;
            float deltaY = adjustedTarget.Y - localPos.Y;
            float deltaZ = adjustedTarget.Z - localPos.Z;

            float horizontalDistance = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);

            // Die Formel selbst bleibt im Kern gleich, aber die Ausrichtung 
            // kann pro Spiel über Parameter justiert werden
            float yaw = MathF.Atan2(deltaY, deltaX) * (180f / MathF.PI) + 90f;
            float pitch = MathF.Atan2(deltaZ, horizontalDistance) * (180f / MathF.PI);

            return (pitch, yaw);
        }
    }
}