using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Arctron.Obj2Gltf
{
    /// <summary>
    /// 3-d point or verctor
    /// </summary>
    public struct Vec3
    {
        public Vec3(Double xyz) : this(xyz, xyz, xyz) { }

        public Vec3(Double x, Double y) : this(x, y, 0.0) { }

        public Vec3(Double x, Double y, Double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Double X;

        public Double Y;

        public Double Z;

        public override String ToString()
        {
            return $"{X}, {Y}, {Z}";
        }

        public Double GetLength()
        {
            return Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        public Vec3 Normalize()
        {
            var len = GetLength();
            return new Vec3(X / len, Y / len, Z / len);
        }

        public Vec3 Substract(Vec3 p)
        {
            return new Vec3(X - p.X, Y - p.Y, Z - p.Z);
        }

        public Vec3 MultiplyBy(Double val)
        {
            return new Vec3(X * val, Y * val, Z * val);
        }

        public Vec3 DividedBy(Double val)
        {
            return new Vec3(X / val, Y / val, Z / val);
        }

        public Byte[] ToFloatBytes()
        {
            return BitConverter.GetBytes((Single)X).Concat(BitConverter.GetBytes((Single)Y)).Concat(BitConverter.GetBytes((Single)Z)).ToArray();
        }

        public static Vec3 Multiply(Vec3 left, Vec3 right)
        {
            return new Vec3(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
        }

        public static Vec3 Add(Vec3 v1, Vec3 v2)
        {
            return new Vec3(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }

        public static Vec3 Cross(Vec3 left, Vec3 right)
        {
            var leftX = left.X;
            var leftY = left.Y;
            var leftZ = left.Z;
            var rightX = right.X;
            var rightY = right.Y;
            var rightZ = right.Z;

            var x = leftY * rightZ - leftZ * rightY;
            var y = leftZ * rightX - leftX * rightZ;
            var z = leftX * rightY - leftY * rightX;

            return new Vec3(x, y, z);
        }

        public static Double Dot(Vec3 v1, Vec3 v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }

        public Double[] ToArray()
        {
            return new[] { X, Y, Z };
        }

    }
}
