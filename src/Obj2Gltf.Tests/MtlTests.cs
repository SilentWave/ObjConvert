using System;
using SilentWave.Obj2Gltf.WaveFront;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SilentWave.Obj2Gltf.Tests
{
    [TestClass]
    public class MtlTests
    {
        [TestMethod]
        public void Number_of_materials_should_match()
        {
            const String mtlStart = "newmtl";
            var materialCount = 0;
            var mtlFile = @"..\..\..\..\testassets\Office\model.mtl";
            Assert.IsTrue(System.IO.File.Exists(mtlFile), "mtl file does not exist!");
            using (var sr = System.IO.File.OpenText(mtlFile))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    if (line.StartsWith(mtlStart)) materialCount++;
                }
            }
            var mtlParser = new MtlParser();
            var mats = mtlParser.Parse(mtlFile);
            Assert.IsTrue(mats.Count() == materialCount);
        }

        [TestMethod]
        public void LoadMtl_should_be_globalization_independent()
        {
            var mtlFile = @"..\..\..\..\testassets\Office\model.mtl";
            Assert.IsTrue(System.IO.File.Exists(mtlFile), "mtl file does not exist!");

            var cultures = System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.NeutralCultures);
            foreach (var culture in cultures)
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = culture;

                var mtlParser = new MtlParser();
                var mats = mtlParser.Parse(mtlFile);
                Assert.IsTrue(mats.Any());
                var mat = mats.Single(x => x.Name == "Mat");
                Assert.IsTrue(mat.Diffuse.Color.Red == 0.80000001192093);
                Assert.IsTrue(mat.Diffuse.Color.Green == 0.80000001192093);
                Assert.IsTrue(mat.Diffuse.Color.Blue == 0.80000001192093);
            }
        }

        // [TestMethod]
        //public void Convert_materials()
        //{
        //    var mtlFile = @"C:\Users\luigi.trabacchin\Desktop\testoutput\exported bueno canada.mtl";
        //    Assert.True(System.IO.File.Exists(mtlFile), "mtl file does not exist!");
        //    var mtlParser = new MtlParser();
        //    var mats = mtlParser.Parse(mtlFile).Select(x => Obj2Gltf.Converter.ConvertMaterial(x, p => 0)).ToArray();
        //    var asd = mats.Select(x => new { Index = Array.IndexOf(mats, x), Material = x })
        //        .GroupBy(
        //            x => new
        //            {
        //                r = x.Material.PbrMetallicRoughness.BaseColorFactor[0],
        //                g = x.Material.PbrMetallicRoughness.BaseColorFactor[1],
        //                b = x.Material.PbrMetallicRoughness.BaseColorFactor[2]
        //            })
        //        .OrderByDescending(x => x.Count())
        //        .ToArray();
        //    var lol = asd.First().Select(x => x.ToString()).ToArray();
        //    Assert.True(mats.Any());
        //}
    }
}
