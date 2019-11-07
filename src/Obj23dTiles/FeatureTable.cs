using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Arctron.Obj23dTiles
{
    /// <summary>
    /// b3dm feature table
    /// 
    /// </summary>
    public class FeatureTable
    {
        /// <summary>
        /// The number of distinguishable models, also called features, in the batch.
        /// If the Binary glTF does not have a batchId attribute, this field must be 0.
        /// </summary>
        [JsonProperty("BATCH_LENGTH")]
        public UInt32 BatchLength { get; set; }
        /// <summary>
        /// A 3-component array of numbers defining the center position
        /// when positions are defined relative-to-center
        /// </summary>
        [JsonProperty("RTC_CENTER")]
        public Single[] RtcCenter { get; set; }
    }
}
