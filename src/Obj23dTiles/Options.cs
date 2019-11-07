using System;
using System.Collections.Generic;
using System.Text;
using Arctron.Obj2Gltf;

namespace Arctron.Obj23dTiles
{
    /// <summary>
    /// b3dm converting options
    /// </summary>
    public class Options
    {
        /// <summary>
        /// FeatureTableJson
        /// </summary>
        public List<Byte> FeatureTableJson { get; set; } = new List<Byte>();
        /// <summary>
        /// FeatureTableBinary
        /// </summary>
        public List<Byte> FeatureTableBinary { get; set; } = new List<Byte>();
        /// <summary>
        /// BatchTableJson
        /// </summary>
        public List<Byte> BatchTableJson { get; set; } = new List<Byte>();
        /// <summary>
        /// BatchTableBinary
        /// </summary>
        public List<Byte> BatchTableBinary { get; set; } = new List<Byte>();
    }
}
