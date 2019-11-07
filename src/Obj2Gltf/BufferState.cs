using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Arctron.Obj2Gltf
{
    /// <summary>
    /// cached all buffers
    /// </summary>
    public class BufferState
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="withBatchTable">whether include batchtables for 3d tiles</param>
        public BufferState(Boolean withBatchTable = false)
        {
            if (withBatchTable)
            {
                BatchIdAccessors = new List<Int32>();
                BatchIdBuffers = new List<Byte[]>();
                BatchTableJson = new BatchTable();
            }
        }
        /// <summary>
        /// Vertex Coordinates Buffers
        /// </summary>
        public List<Byte[]> PositionBuffers { get; } = new List<Byte[]>();
        /// <summary>
        /// Vertex Normals Buffers
        /// </summary>
        public List<Byte[]> NormalBuffers { get; } = new List<Byte[]>();
        /// <summary>
        /// Vertex Texture Coordinates Buffers
        /// </summary>
        public List<Byte[]> UvBuffers { get; } = new List<Byte[]>();
        /// <summary>
        /// Triangle Indices Buffers
        /// </summary>
        public List<Byte[]> IndexBuffers { get; } = new List<Byte[]>();
        /// <summary>
        /// Vertex Coordinates Indices
        /// </summary>
        public List<Int32> PositionAccessors { get; } = new List<Int32>();
        /// <summary>
        /// Vertex Normals Indices
        /// </summary>
        public List<Int32> NormalAccessors { get; } = new List<Int32>();
        /// <summary>
        /// Vertex Texture Coordinates Indices
        /// </summary>
        public List<Int32> UvAccessors { get; } = new List<Int32>();
        /// <summary>
        /// Triangle Indices
        /// </summary>
        public List<Int32> IndexAccessors { get; } = new List<Int32>();
        /// <summary>
        /// if with batchTable, the current batch id
        /// </summary>
        public Int32 CurrentBatchId { get; set; }
        /// <summary>
        /// if with batchTable, batch ids buffers
        /// </summary>
        public List<Byte[]> BatchIdBuffers { get; set; }
        /// <summary>
        /// if with batchTable, batch ids indices
        /// </summary>
        public List<Int32> BatchIdAccessors { get; set; }
        /// <summary>
        /// batched table
        /// </summary>
        public BatchTable BatchTableJson { get; set; }
    }
}
