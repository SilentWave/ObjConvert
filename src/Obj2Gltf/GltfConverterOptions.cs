using System;
using System.Text;

namespace SilentWave.Obj2Gltf
{
    public class GltfConverterOptions
    {
        /// <summary>
        /// obj and mtl files' text encoding
        /// </summary>
        public Encoding ObjEncoding { get; set; }

        public Boolean RemoveDegenerateFaces { get; set; }
    }
}
