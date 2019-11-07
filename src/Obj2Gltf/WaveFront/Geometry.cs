using System;
using System.Collections.Generic;
using System.IO;

namespace Arctron.Obj2Gltf.WaveFront
{
    /// <summary>
    /// geometry with face meshes
    /// http://paulbourke.net/dataformats/obj/
    /// http://www.fileformat.info/format/wavefrontobj/egff.htm
    /// </summary>
    public class Geometry
    {
        /// <summary>
        /// group name
        /// </summary>
        public String Id { get; set; }
        /// <summary>
        /// meshes
        /// </summary>
        public List<Face> Faces { get; set; } = new List<Face>();
        /// <summary>
        /// write geometry
        /// </summary>
        /// <param name="writer"></param>
        public void Write(StreamWriter writer)
        {
            writer.WriteLine($"g {Id}");
            writer.WriteLine($"s off");
            foreach (var f in Faces)
            {
                f.Write(writer);
            }
        }
    }
}
