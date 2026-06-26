using System;
using System.Numerics;

namespace MemoryEngine
{
    public struct ViewMatrixAssaultCube
    {
        public float[] m;

        public static ViewMatrixAssaultCube FromBytes(byte[] bytes)
        {
            ViewMatrixAssaultCube matrix = new ViewMatrixAssaultCube { m = new float[16] };
            for (int i = 0; i < 16; i++)
            {
                matrix.m[i] = BitConverter.ToSingle(bytes, i * 4);
            }
            return matrix;
        }

        public static bool WorldToScreen(Vector3 pos, out Vector2 screenPos, ViewMatrixAssaultCube matrix, int width, int height)
        {
            float[] m = matrix.m;
            // Standard AC Projektion
            float w = m[3] * pos.X + m[7] * pos.Y + m[11] * pos.Z + m[15];

            if (w < 0.01f) { screenPos = new Vector2(0, 0); return false; }

            float nx = (m[0] * pos.X + m[4] * pos.Y + m[8] * pos.Z + m[12]) / w;
            float ny = (m[1] * pos.X + m[5] * pos.Y + m[9] * pos.Z + m[13]) / w;

            // In Bildschirm-Koordinaten umrechnen
            screenPos.X = (width / 2f) * (nx + 1f);
            screenPos.Y = (height / 2f) * (1f - ny);
            return true;
        }
    }
}