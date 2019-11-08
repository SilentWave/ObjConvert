using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.IO;

namespace Arctron.Obj2Gltf.Tests
{
    public class ConverterTests
    {
        private static String TestObjPath = @"..\..\..\..\testassets\Office\model.obj";

        static void CheckObjFiles()
        {
            Assert.True(File.Exists(TestObjPath), "Obj File does not exist!");
        }
        [Fact]
        public void TestConvertGltf()
        {
            var name = "model";
            CheckObjFiles();
            var options = new GltfOptions();
            var mtlParser = new WaveFront.MtlParser();
            var objParser = new WaveFront.ObjParser();
            var converter = new Converter(objParser, mtlParser);
            var outputFile = name + ".gltf";
            var (model, _) = converter.Convert(TestObjPath, options);
            converter.WriteFile(model, false, outputFile);
            Assert.True(File.Exists(outputFile));
        }

        [Fact]
        public void TestConvertGltf2()
        {
            var name = "model";
            CheckObjFiles();

            var mtlParser = new WaveFront.MtlParser();
            var objParser = new WaveFront.ObjParser();
            var objModel = objParser.Parse(TestObjPath);

            var converter = new Converter(objParser, mtlParser);
            var outputFile = name + ".gltf";
            var (model, buffers) = converter.Convert(objModel, Path.GetDirectoryName(TestObjPath), new GltfOptions { Name = name });
            converter.WriteFile(model, false, outputFile, buffers);
            Assert.True(File.Exists(outputFile));
        }

        [Fact]
        public void TestConvertGlb()
        {
            var name = "model";

            CheckObjFiles();
            var objFile = TestObjPath;
            var options = new GltfOptions { Binary = true };
            var mtlParser = new WaveFront.MtlParser();
            var objParser = new WaveFront.ObjParser();
            var converter = new Converter(objParser, mtlParser);
            var outputFile = $"{name}.glb";
            var (model, buffers) = converter.Convert(objFile, options);
            converter.WriteFile(model, true, outputFile, buffers);
            Assert.True(System.IO.File.Exists(outputFile));
        }
    }
}
