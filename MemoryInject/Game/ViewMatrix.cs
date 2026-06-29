using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MemoryEngine.Game
{
    public struct MatrixSettings
    {
        public bool IsColumnMajor;
        public bool IsZeroToOneRange;
        public bool InvertY;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ViewMatrix
    {
        // Festes Array statt dynamisch (keine GC-Lags mehr!)
        public unsafe fixed float m[16];
        public MatrixSettings Settings;

        public bool WorldToScreen(Vector3 pos, out Vector2 screenPos, int width, int height)
        {
            // Wir nutzen das fixed-Statement, um sicher auf das Array zuzugreifen
            unsafe
            {
                fixed (float* pMatrix = m)
                {
                    // Wenn es ColumnMajor ist, transponieren wir "on the fly"
                    // Für maximale Performance hier, arbeiten wir direkt mit dem Pointer
                    float* mat = Settings.IsColumnMajor ? TransposeInternal() : pMatrix;

                    float w = mat[3] * pos.X + mat[7] * pos.Y + mat[11] * pos.Z + mat[15];

                    if (w < 0.01f) { screenPos = Vector2.Zero; return false; }

                    float nx = (mat[0] * pos.X + mat[4] * pos.Y + mat[8] * pos.Z + mat[12]) / w;
                    float ny = (mat[1] * pos.X + mat[5] * pos.Y + mat[9] * pos.Z + mat[13]) / w;

                    float xOffset = Settings.IsZeroToOneRange ? 0f : 1f;
                    screenPos.X = (width / 2f) * (nx + xOffset);
                    screenPos.Y = Settings.InvertY ? (height / 2f) * (1f - ny) : (height / 2f) * (1f + ny);

                    return true;
                }
            }
        }

        private unsafe float* TransposeInternal()
        {
            float* t = stackalloc float[16];
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    t[i * 4 + j] = m[j * 4 + i];
            return t;
        }
    }
}