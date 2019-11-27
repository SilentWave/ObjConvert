using System;
using System.Collections.Generic;
using System.Text;

namespace SilentWave
{
    public class Gltf2GlbOptions
    {
        public Gltf2GlbOptions(String inputPath, String outputPath = null)
        {
            InputPath = inputPath ?? throw new ArgumentNullException();
            OutPutPath = outputPath ?? System.IO.Path.Combine(System.IO.Path.GetDirectoryName(inputPath), System.IO.Path.GetFileNameWithoutExtension(inputPath) + ".glb");
        }

        public String InputPath { get; set; }
        public Boolean MinifyJson { get; set; } = true;

        // TODO: make this optional ?
        //public Boolean EmbedBuffers { get; set; } = true;
        //public Boolean EmbedImages { get; set; } = true;

        public Boolean DeleteOriginal { get; set; } = false;
        public String OutPutPath { get; set; }
    }
}
