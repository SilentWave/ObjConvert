using System;
using System.Collections.Generic;
using System.Text;

namespace Arctron.Obj23dTiles
{
    /// <summary>
    /// b3dm file format model
    /// </summary>
    public class B3dm
    {
        internal const Int32 Version = 1;

        internal const Int32 HeaderByteLength = 28;

        private readonly List<Byte> _glb;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="glb">binary gltf model</param>
        public B3dm(List<Byte> glb)
        {
            _glb = glb;
        }
        /// <summary>
        /// convert to b3dm binary data
        /// </summary>
        /// <param name="options">converting options</param>
        /// <returns></returns>
        public Byte[] Convert(Options options)
        {
            if (options == null) options = new Options();

            var featureTableJson = options.FeatureTableJson;
            var featureTableBinary = options.FeatureTableBinary;
            var batchTableJson = options.BatchTableJson;
            var batchTableBinary = options.BatchTableBinary;

            var featureTableJsonByteLength = featureTableJson.Count;
            var featureTableBinaryByteLength = featureTableBinary.Count;
            var batchTableJsonByteLength = batchTableJson.Count;
            var batchTableBinaryByteLength = batchTableBinary.Count;
            var gltfByteLength = _glb.Count;
            var byteLength = HeaderByteLength + featureTableJsonByteLength 
                + featureTableBinaryByteLength + batchTableJsonByteLength 
                + batchTableBinaryByteLength + gltfByteLength;

            var all = new List<Byte>();
            // Header
            all.Add(System.Convert.ToByte('b'));
            all.Add(System.Convert.ToByte('3'));
            all.Add(System.Convert.ToByte('d'));
            all.Add(System.Convert.ToByte('m'));
            all.AddRange(BitConverter.GetBytes((UInt32)Version));
            all.AddRange(BitConverter.GetBytes((UInt32)byteLength));
            all.AddRange(BitConverter.GetBytes((UInt32)featureTableJsonByteLength));
            all.AddRange(BitConverter.GetBytes((UInt32)featureTableBinaryByteLength));
            all.AddRange(BitConverter.GetBytes((UInt32)batchTableJsonByteLength));
            all.AddRange(BitConverter.GetBytes((UInt32)batchTableBinaryByteLength));

            all.AddRange(featureTableJson);
            all.AddRange(featureTableBinary);
            all.AddRange(batchTableJson);
            all.AddRange(batchTableBinary);
            all.AddRange(_glb);

            return all.ToArray();
        }
    }
}
