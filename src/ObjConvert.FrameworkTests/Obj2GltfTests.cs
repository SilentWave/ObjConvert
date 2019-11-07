
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Arctron.Obj2Gltf;

namespace Arctron.ObjConvert.FrameworkTests
{
    public class Obj2GltfTests
    {
        static String Name = "model";
        internal static String TestObjFile = $@"..\..\..\testassets\Office\{Name}.obj";

        public static void TestConvert()
        {
            var objFile = TestObjFile;
            var opts = new GltfOptions();
            var mtlParser = new Obj2Gltf.WaveFront.MtlParser();
            var objParser = new Obj2Gltf.WaveFront.ObjParser();
            var converter = new Converter( objParser, mtlParser);
           var (model, _) = converter.Convert(objFile, opts);
            var outputFile = $"{Name}.gltf";
            if (opts.Binary)
            {
                outputFile = $"{Name}.glb";
            }
           
            converter.WriteFile(model, false, outputFile);
        }
    }
}
