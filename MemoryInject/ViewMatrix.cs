using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MemoryEngine
{
    public struct ViewMatrix
    {

        public float M11, M12, M13, M14;
        public float M21, M22, M23, M24;
        public float M31, M32, M33, M34;
        public float M41, M42, M43, M44;


        public static bool WorldToScreen(Vector3 pos, out Vector2 screenPos, ViewMatrix matrix, int width, int height)
        {
            float clipX = pos.X * matrix.M11 + pos.Y * matrix.M21 + pos.Z * matrix.M31 + matrix.M41;
            float clipY = pos.X * matrix.M12 + pos.Y * matrix.M22 + pos.Z * matrix.M32 + matrix.M42;
            float clipW = pos.X * matrix.M14 + pos.Y * matrix.M24 + pos.Z * matrix.M34 + matrix.M44;

            if (clipW < 0.1f) { screenPos = new Vector2(0, 0); return false; }

            float ndcX = clipX / clipW;
            float ndcY = clipY / clipW;

            screenPos.X = (width / 2 * ndcX) + (ndcX + width / 2);
            screenPos.Y = -(height / 2 * ndcY) + (ndcY + height / 2);

            return true;
        }
    }
}