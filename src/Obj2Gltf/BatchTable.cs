using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Arctron.Obj2Gltf
{
    /// <summary>
    /// batched table
    /// </summary>
    public class BatchTable
    {
        /// <summary>
        /// Batch Ids in ushort
        /// </summary>
        [JsonProperty("batchId")]
        public List<UInt16> BatchIds { get; set; } = new List<UInt16>();
        /// <summary>
        /// Batch Names
        /// </summary>
        [JsonProperty("name")]
        public List<String> Names { get; set; } = new List<String>();
        /// <summary>
        /// The maximum boundary for each batch
        /// </summary>
        [JsonProperty("maxPoint")]
        public List<Double[]> MaxPoint { get; set; } = new List<Double[]>();
        /// <summary>
        /// The minimum boundary for each batch
        /// </summary>
        [JsonProperty("minPoint")]
        public List<Double[]> MinPoint { get; set; } = new List<Double[]>();
    }
}
