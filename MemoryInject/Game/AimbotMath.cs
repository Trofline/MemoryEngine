using System;
using System.Numerics;

namespace MemoryEngine.Game
{
    public static class AimbotMath
    {
        /// <summary>
        /// Berechnet die Ziel-Winkel (Pitch und Yaw) von der eigenen Position zum Ziel.
        /// </summary>
        /// <param name="localPos">Deine eigene 3D-Position (X, Y, Z)</param>
        /// <param name="targetPos">Die 3D-Position des Gegners (X, Y, Z)</param>
        /// <returns>Ein Tupel aus (Pitch, Yaw)</returns>
        public static (float pitch, float yaw) CalculateAngle(Vector3 localPos, Vector3 targetPos)
        {
            float deltaX = targetPos.X - localPos.X;
            float deltaY = targetPos.Y - localPos.Y;
            float deltaZ = targetPos.Z - localPos.Z;

            // Horizontale Distanz (auf der X/Y-Ebene)
            float horizontalDistance = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);

            // Yaw-Berechnung (Links / Rechts)
            // Je nach Spiel muss hier evtl. ein Offset von +90 oder -90 draufgerechnet werden
            float yaw = MathF.Atan2(deltaY, deltaX) * (180f / MathF.PI) + 90f;

            // Pitch-Berechnung (Hoch / Runter)
            float pitch = MathF.Atan2(deltaZ, horizontalDistance) * (180f / MathF.PI);

            return (pitch, yaw);
        }
    }
}