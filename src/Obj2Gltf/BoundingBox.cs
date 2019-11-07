﻿using System;
using System.Collections.Generic;

namespace Arctron.Obj2Gltf
{
    public class BoundingBox
    {
        public DoubleRange X { get; set; } = new DoubleRange();

        public DoubleRange Y { get; set; } = new DoubleRange();

        public DoubleRange Z { get; set; } = new DoubleRange();

        public Boolean IsIn(Vec3 p)
        {
            return p.X >= X.Min && p.X <= X.Max &&
                p.Y >= Y.Min && p.Y <= Y.Max &&
                p.Z >= Z.Min && p.Z <= Z.Max;
        }

        public override String ToString()
        {
            return $"X: {X}; Y: {Y}; Z: {Z}";
        }

        public List<BoundingBox> Split(Int32 level)
        {
            if (level <= 1) return new List<BoundingBox> { this };
            var boxes = new List<BoundingBox>();
            var diffX = (X.Max - X.Min) / level;
            var diffY = (Y.Max - Y.Min) / level;
            var diffZ = (Z.Max - Z.Min) / level;
            for(var x = 0;x<level;x++)
            {
                var xj = x + 1;
                var maxX = X.Max;
                if (xj < level)
                {
                    maxX = X.Min + xj * diffX;
                }
                for(var y =0;y<level;y++)
                {
                    var yj = y + 1;
                    var maxY = Y.Max;
                    if (yj < level)
                    {
                        maxY = Y.Min + yj * diffY;
                    }

                    for(var z = 0;z<level;z++)
                    {
                        var zj = z + 1;
                        var maxZ = Z.Max;
                        if (zj < level)
                        {
                            maxZ = Z.Min + zj * diffZ;
                        }

                        boxes.Add(new BoundingBox
                        {
                            X = new DoubleRange { Min = X.Min + x * diffX, Max = maxX },
                            Y = new DoubleRange { Min = Y.Min + y * diffY, Max = maxY },
                            Z = new DoubleRange { Min = Z.Min + z * diffZ, Max = maxZ }
                        });
                    }
                }
            }
            return boxes;
        }
    }
}
