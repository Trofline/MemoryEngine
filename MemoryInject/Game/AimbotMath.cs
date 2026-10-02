using System;
using System.Numerics;

namespace MemoryEngine.Game
{
    public static class AimbotMath
    {
        private static readonly Dictionary<IntPtr, Vector3> _lastTargetPositions = new Dictionary<IntPtr, Vector3>();

        public static (float pitch, float yaw) CalculateAngle(Vector3 localPos, Vector3 targetPos, IntPtr entityKey = default, bool prediction = false, float zOffset = 0.5f)
        {
            Vector3 finalTarget = new Vector3(targetPos.X, targetPos.Y, targetPos.Z + zOffset);

            if (prediction && entityKey != default)
            {
                if (_lastTargetPositions.TryGetValue(entityKey, out Vector3 lastPos))
                {
                    Vector3 velocity = finalTarget - lastPos;
                    float predictionFactor = 1.5f; // Kann man hier festlegen oder übergeben
                    finalTarget += velocity * predictionFactor;
                }
                _lastTargetPositions[entityKey] = finalTarget;
            }

            float deltaX = finalTarget.X - localPos.X;
            float deltaY = finalTarget.Y - localPos.Y;
            float deltaZ = finalTarget.Z - localPos.Z;

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