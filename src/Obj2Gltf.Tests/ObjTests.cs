using System;
using System.Collections.Generic;
using System.Text;
using SilentWave.Obj2Gltf.WaveFront;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SilentWave.Obj2Gltf.Tests
{
    [TestClass]
    public class ObjTests
    {
        static String objFile = @"..\..\..\..\testassets\Office\model.obj";

        [TestMethod]
        public void Test_LoadObj()
        {
            objFile = @"..\..\..\..\testassets\CubeWithGroups\CubeWithGroups.obj";
            Assert.IsTrue(System.IO.File.Exists(objFile), "obj file does not exist!");
            var parser = new ObjParser();
            var model = parser.Parse(objFile);

            var knownGroups = new String[] { "cube", "front", "back", "right", "top", "left", "bottom" };
            foreach (var group in knownGroups)
            {
                Assert.IsTrue(model.Geometries.Any(x => x.Id == group));
            }

            Assert.IsTrue(model.Vertices.Count == 8);
        }
    }
}
