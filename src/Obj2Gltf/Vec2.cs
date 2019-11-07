using System;
using System.Linq;

namespace Arctron.Obj2Gltf
{
    /// <summary>
    /// 2-d point or vector
    /// </summary>
    public struct Vec2
    {
        public Vec2(Double u, Double v)
        {
            U = u;
            V = v;
        }

        public Double U;

        public Double V;

        public override String ToString()
        {
            return $"{U}, {V}";
        }

        public Byte[] ToFloatBytes()
        {            
            return BitConverter.GetBytes((Single)U).Concat(BitConverter.GetBytes((Single)V)).ToArray();
        }

        public Double[] ToArray()
        {
            return new[] { U, V };
        }

        public Double GetDistance(Vec2 p)
        {
            return Math.Sqrt((U - p.U) * (U - p.U) + (V - p.V) * (V - p.V));
        }

        public Double GetLength()
        {
            return Math.Sqrt(U * U + V * V);
        }

        public Vec2 Normalize()
        {
            var len = GetLength();
            return new Vec2(U / len, V / len);
        }

        public Double Dot(Vec2 v)
        {
            return U * v.U + V * v.V;
        }
    }
}
