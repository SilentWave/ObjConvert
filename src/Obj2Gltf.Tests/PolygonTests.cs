using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Arctron.Obj2Gltf.Geom;

namespace Arctron.Obj2Gltf.Tests
{
    public class PolygonTests
    {
        [Fact]
        public void Test_Intersect()
        {
            var pnts = new List<SVec2>
            {
                new SVec2(0.0f, 0.0f), new SVec2(1.0f, 0.0f), new SVec2(1.0f,1.0f), new SVec2(0.5f,0.5f), new SVec2(0.0f,1.0f)
            };
            var tol = 1e-8f;
            Assert.Equal(PolygonPointRes.Vertex, PolygonUtil.CrossTest(new SVec2(0.0f, 0.0f), pnts, tol));
            Assert.Equal(PolygonPointRes.Edge, PolygonUtil.CrossTest(new SVec2(0.1f, 0.0f), pnts, tol));
            Assert.Equal(PolygonPointRes.Outside, PolygonUtil.CrossTest(new SVec2(0.5f, 0.6f), pnts, tol));
            Assert.Equal(PolygonPointRes.Outside, PolygonUtil.CrossTest(new SVec2(0.5f, 0.500001f), pnts, tol));
            Assert.Equal(PolygonPointRes.Inside, PolygonUtil.CrossTest(new SVec2(0.5f, 0.499999f), pnts, tol));
            Assert.Equal(PolygonPointRes.Outside, PolygonUtil.CrossTest(new SVec2(1.5f, 0.5f), pnts, tol));
        }

        [Fact]
        public void Test_BoundingBoxSplit()
        {
            var box = new BoundingBox
            {
                X = new SingleRange { Min = 0, Max = 10 },
                Y = new SingleRange { Min = 0, Max = 10 },
                Z = new SingleRange { Min = 0, Max = 10 }
            };

            var boxes = box.Split(2);
            Assert.Equal(8, boxes.Count);
        }
    }
}
