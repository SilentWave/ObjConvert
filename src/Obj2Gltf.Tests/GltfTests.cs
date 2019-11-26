using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace SilentWave.Obj2Gltf.Tests
{
    [TestClass]
    public class GltfTests
    {

        [TestMethod]
        public void Test_Load_Gltf()
        {
            var file = @"..\..\..\..\testassets\Office\model.gltf";
            Assert.IsTrue(System.IO.File.Exists(file), "gltf file does not exist!");
            var model = Gltf.GltfModel.LoadFromJsonFile(file);
            Assert.IsTrue(model != null);
        }
    }
}
