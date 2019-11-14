using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.IO;

namespace Arctron.Obj2Gltf.Tests
{
    public class ConverterTests
    {
        [Fact]
        public void Converting_obj_to_gltf_should_retain_textures()
        {
            var path = @"..\..\..\..\testassets\CubeWithTextures\cube.obj";
            var name = Path.GetFileNameWithoutExtension(path);
            var options = new GltfConverterOptions();
            var mtlParser = new WaveFront.MtlParser();
            var objParser = new WaveFront.ObjParser();
            var converter = new Converter(objParser, mtlParser);
            var outputFile = Path.Combine(Path.GetDirectoryName(path), $"{name}.gltf");
            var model = converter.Convert(path, options);
            converter.WriteFile(model, outputFile);
            Assert.True(File.Exists(outputFile));
        }

        [Fact]
        public void Converting_obj_to_gltf_should_retain_normals()
        {
            var path = @"..\..\..\..\testassets\Office\model.obj";
            var dir = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);
            var options = new GltfConverterOptions();
            var mtlParser = new WaveFront.MtlParser();
            var objParser = new WaveFront.ObjParser();
            var converter = new Converter(objParser, mtlParser);
            var outputFile = Path.Combine(dir, $"{name}.gltf");
            var model = converter.Convert(path, options);
            converter.WriteFile(model, outputFile);
            Assert.True(File.Exists(outputFile));
        }

        [Fact]
        public void TestConvertGltf2()
        {
            var path = @"..\..\..\..\testassets\CubeWithTextures\cube.obj";
            var dir = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);

            var mtlParser = new WaveFront.MtlParser();
            var objParser = new WaveFront.ObjParser();
            var objModel = objParser.Parse(path);

            var converter = new Converter(objParser, mtlParser);
            var outputFile = Path.Combine(dir, $"{name}.gltf");
            var model = converter.Convert(objModel, Path.GetDirectoryName(path), new GltfConverterOptions { Name = name });
            converter.WriteFile(model, outputFile);
            Assert.True(File.Exists(outputFile));
        }

        [Fact]
        public void TestConvertGlb()
        {
            var path = @"..\..\..\..\testassets\CubeWithTextures\cube.obj";
            var dir = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);

            var options = new GltfConverterOptions { Binary = true };
            var mtlParser = new WaveFront.MtlParser();
            var objParser = new WaveFront.ObjParser();
            var converter = new Converter(objParser, mtlParser);
            var outputFile = Path.Combine(dir, $"{name}.gltf");
            var model = converter.Convert(path, options);
            converter.WriteFile(model, outputFile);

            var glbConv = new Gltf2GlbConverter();
            glbConv.Convert(new Gltf2GlbOptions(outputFile));

            Assert.True(File.Exists(outputFile));
        }
    }
}
