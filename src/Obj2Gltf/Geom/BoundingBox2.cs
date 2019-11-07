﻿using System;

namespace Arctron.Obj2Gltf.Geom
{
    public class BoundingBox2
    {
        public Vec2 Min { get; set; }

        public Vec2 Max { get; set; }

        public Boolean IsValid()
        {
            return Min.U < Max.U && Min.V < Max.V;
        }

        public Boolean IsIn(Vec2 p)
        {
            return p.U > Min.U && p.U < Max.U && p.V > Min.V && p.V < Max.V;
        }

        public static BoundingBox2 New()
        {
            return new BoundingBox2
            {
                Min = new Vec2(Double.MaxValue, Double.MaxValue),
                Max = new Vec2(Double.MinValue, Double.MinValue)
            };

        }
    }
}
