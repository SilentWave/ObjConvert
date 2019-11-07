using System;
using System.Text;

namespace Arctron.Obj2Gltf
{
    public class GltfOptions
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
        /// whether to generate batchids
        /// </summary>
        public Boolean WithBatchTable { get; set; }
        /// <summary>
        /// obj and mtl files' text encoding
        /// </summary>
        public Encoding ObjEncoding { get; set; }
    }
}
