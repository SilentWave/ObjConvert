using System;
using System.Collections.Generic;
using System.Text;

namespace Arctron.Obj2Gltf.WaveFront
{
    static class Extensions
    {
        public static Boolean RequiresUint32Indices(this ObjModel objModel) => objModel.Vertices.Count > 65534;
    }
}
