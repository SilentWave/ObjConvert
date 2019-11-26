using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SilentWave.Obj2Gltf.Tests
{
    [TestClass]
    public class ConverterTests
    {
        [TestMethod]
        public void Converting_obj_to_gltf_should_retain_textures()
        {
            var path = @"..\..\..\..\testassets\CubeWithTextures\cube.obj";
            var name = Path.GetFileNameWithoutExtension(path);

            var converter = Converter.MakeDefault();
            var outputFile = Path.Combine(Path.GetDirectoryName(path), $"{name}.gltf");
            converter.Convert(path, outputFile);
            Assert.IsTrue(File.Exists(outputFile));
        }

        [TestMethod]
        public void Converting_obj_to_gltf_should_retain_normals()
        {
            var path = @"..\..\..\..\testassets\Office\model.obj";
            var dir = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);

            var converter = Converter.MakeDefault();
            var outputFile = Path.Combine(dir, $"{name}.gltf");
            converter.Convert(path, outputFile);
            Assert.IsTrue(File.Exists(outputFile));
        }

        [TestMethod]
        public void TestConvertGltf2()
        {
            var path = @"..\..\..\..\testassets\CubeWithTextures\cube.obj";
            var dir = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);

            var converter = Converter.MakeDefault();
            var outputFile = Path.Combine(dir, $"{name}.gltf");
            converter.Convert(path, outputFile);
            Assert.IsTrue(File.Exists(outputFile));
        }

        [TestMethod]
        public void TestConvertGlb()
        {
            var objPath = @"..\..\..\..\testassets\CubeWithTextures\cube.obj";
            var dir = Path.GetDirectoryName(objPath);
            var name = Path.GetFileNameWithoutExtension(objPath);

            var converter = Converter.MakeDefault();
            var outputFile = Path.Combine(dir, $"{name}.gltf");
            converter.Convert(objPath, outputFile);

            var glbConv = new Gltf2GlbConverter();
            glbConv.Convert(new Gltf2GlbOptions(outputFile));

            Assert.IsTrue(File.Exists(outputFile));
        }
    }
}
