using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Arctron.Obj2Gltf.WaveFront;
using System.Linq;

namespace Arctron.Obj2Gltf.Tests
{
    public class ObjTests
    {
        static String objFile = @"..\..\..\..\testassets\Office\model.obj";

        [Fact]
        public void Test_LoadObj()
        {
            objFile = @"..\..\..\..\testassets\CubeWithGroups\CubeWithGroups.obj";
            Assert.True(System.IO.File.Exists(objFile), "obj file does not exist!");
            var parser = new ObjParser();
            var model = parser.Parse(objFile);

            var knownGroups = new String[] { "cube", "front", "back", "right", "top", "left", "bottom" };
            foreach (var group in knownGroups)
            {
                Assert.Contains(model.Geometries, x => x.Id == group);
            }
            Assert.True(model.Vertices.Count == 8);
        }

        [Fact]
        public void Test_Split()
        {
            Assert.True(System.IO.File.Exists(objFile), "obj file does not exist!");
            var parser = new ObjParser();
            var model = parser.Parse(objFile);
            var models = model.Split(2);
            Assert.True(models.Count > 0);
        }
    }
}
