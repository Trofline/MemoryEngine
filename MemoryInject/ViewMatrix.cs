using System;
using System.Numerics;

namespace MemoryEngine
{
    public struct MatrixSettings
    {
        public bool IsColumnMajor;    // Manche Spiele speichern Spaltenweise
        public bool IsZeroToOneRange; // Manche Spiele nutzen 0-1 NDC statt -1-1
        public bool InvertY;          // DirectX vs OpenGL Unterschied
    }

    public struct ViewMatrix
    {
        public float[] m;
        public MatrixSettings settings;

        public static ViewMatrix FromBytes(byte[] bytes, MatrixSettings settings)
        {
            ViewMatrix matrix = new ViewMatrix { m = new float[16], settings = settings };
            for (int i = 0; i < 16; i++)
            {
                matrix.m[i] = BitConverter.ToSingle(bytes, i * 4);
            }
            return matrix;
        }

        public bool WorldToScreen(Vector3 pos, out Vector2 screenPos, int width, int height)
        {
            // Wenn das Spiel Column-Major ist, transponieren wir die Matrix virtuell
            float[] mat = settings.IsColumnMajor ? Transpose(m) : m;

            float w = mat[3] * pos.X + mat[7] * pos.Y + mat[11] * pos.Z + mat[15];

            if (w < 0.01f) { screenPos = new Vector2(0, 0); return false; }

            float nx = (mat[0] * pos.X + mat[4] * pos.Y + mat[8] * pos.Z + mat[12]) / w;
            float ny = (mat[1] * pos.X + mat[5] * pos.Y + mat[9] * pos.Z + mat[13]) / w;

            float xOffset = settings.IsZeroToOneRange ? 0f : 1f;
            screenPos.X = (width / 2f) * (nx + xOffset);

            // Y-Invertierung je nach Grafik-API
            screenPos.Y = settings.InvertY ? (height / 2f) * (1f - ny) : (height / 2f) * (1f + ny);

            return true;
        }

        private static float[] Transpose(float[] m)
        {
            float[] t = new float[16];
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    t[i * 4 + j] = m[j * 4 + i];
            return t;
        }
    }
}