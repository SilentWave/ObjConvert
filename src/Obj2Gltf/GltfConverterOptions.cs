using System;
using System.Text;

namespace Arctron.Obj2Gltf
{
    public class GltfConverterOptions
    {
        /// <summary>
        /// Model Name
        /// </summary>
        public String Name { get; set; }
        /// <summary>
        /// glb?
        /// </summary>
        public Boolean Binary { get; set; }
        /// <summary>
        /// obj and mtl files' text encoding
        /// </summary>
        public Encoding ObjEncoding { get; set; }

        public Boolean RemoveDegenerateFaces { get; set; }
    }
}
