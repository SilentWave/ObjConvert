using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Arctron.Obj2Gltf.WaveFront;
using Arctron.Gltf;
using System.IO;
using Newtonsoft.Json;

namespace Arctron.Obj2Gltf
{
    /// <summary>
    /// A delegate to get an existing texture index or add it to the list
    /// </summary>
    /// <param name="texturePath">The path where the texture can be found</param>
    /// <returns>The texture index (zero based)</returns>
    public delegate Int32 GetOrAddTexture(GltfModel gltfModel, String texturePath);

    /// <summary>
    /// obj2gltf converter
    /// </summary>
    public class Converter
    {
        private readonly ObjParser _objParser;
        private readonly IMtlParser _mtlParser;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objPath">obj file path</param>
        /// <param name="options"></param>
        public Converter(ObjParser objParser, IMtlParser mtlParser)
        {
            _objParser = objParser ?? throw new ArgumentNullException(nameof(objParser));
            _mtlParser = mtlParser ?? throw new ArgumentNullException(nameof(mtlParser));
        }

        public (GltfModel Model, List<byte> AllBuffers) Convert(String objPath, GltfOptions options = null)
        {
            if (String.IsNullOrWhiteSpace(objPath))
                throw new ArgumentNullException(nameof(objPath));

            var objModel = _objParser.Parse(objPath, options.ObjEncoding);
            if (!String.IsNullOrWhiteSpace(options.Name))
            {
                objModel.Name = options.Name;
            }
            var objFolder = Path.GetDirectoryName(objPath);
            if (!String.IsNullOrEmpty(objModel.MatFilename))
            {
                var matFile = Path.Combine(objFolder, objModel.MatFilename);

                var mats = _mtlParser.ParseAsync(matFile).Result;
                objModel.Materials.AddRange(mats);
            }
            return Convert(objModel, objFolder, options);
        }

        public (GltfModel Model, List<byte> AllBuffers) Convert(ObjModel objModel, String objFolder, GltfOptions options = null)
        {
            if (objModel == null) throw new ArgumentNullException(nameof(objModel));
            options = options ?? new GltfOptions();
            var bufferState = new BufferState(options.WithBatchTable);


            var gltfModel = new GltfModel();
            gltfModel.Scenes.Add(new Scene());
            var u32IndicesEnabled = RequiresUint32Indices(objModel);
            var meshes = objModel.Geometries.ToArray();
            var meshesLength = meshes.Length;
            for (var i = 0; i < meshesLength; i++)
            {
                var mesh = meshes[i];
                var meshIndex = AddMesh(gltfModel, objModel, bufferState, mesh, u32IndicesEnabled, options);
                AddNode(gltfModel, mesh.Id, meshIndex, null);
            }

            if (gltfModel.Images.Count > 0)
            {
                gltfModel.Samplers.Add(new TextureSampler
                {
                    MagFilter = MagnificationFilterKind.Linear,
                    MinFilter = MinificationFilterKind.NearestMipmapLinear,
                    WrapS = TextureWrappingMode.Repeat,
                    WrapT = TextureWrappingMode.Repeat
                });
            }

            var allBuffers = AddBuffers(gltfModel, bufferState, options);
            gltfModel.Buffers.Add(new Gltf.Buffer
            {
                Name = options.Name,
                ByteLength = allBuffers.Count
            });
            var boundary = 4;
            FillImageBuffers(gltfModel, objFolder, allBuffers, boundary);


            if (!options.Binary)
            {
                gltfModel.Buffers[0].Uri = "data:application/octet-stream;base64," + System.Convert.ToBase64String(allBuffers.ToArray());
            }
            return (gltfModel, allBuffers);
        }

        /// <summary>
        /// write converted data to file
        /// </summary>
        /// <param name="outputFile"></param>
        public void WriteFile(GltfModel gltfModel, Boolean binary, String outputFile, List<Byte> allbuffers = null)
        {
            if (gltfModel == null) throw new ArgumentNullException();
            if (binary)
            {
                if (allbuffers == null) throw new ArgumentNullException(nameof(allbuffers));
                var _glb = GltfToGlb(gltfModel, allbuffers);
                File.WriteAllBytes(outputFile, _glb.ToArray());
            }
            else
            {
                var json = ToJson(gltfModel);
                File.WriteAllText(outputFile, json);
            }
        }

        ///// <summary>
        ///// get batch table if batch table enabled
        ///// </summary>
        ///// <returns></returns>
        //public BatchTable GetBatchTable()
        //{
        //    if (gltfModel == null) Convert();
        //    return buffers.BatchTableJson;
        //}


        private static String ToJson(Object model)
        {
            return JsonConvert.SerializeObject(model,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        Formatting = Formatting.Indented,
                        ContractResolver = new CustomContractResolver()
                    });
        }

        private Boolean CheckWindingCorrect(Vec3 a, Vec3 b, Vec3 c, Vec3 normal)
        {
            var ba = new Vec3(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
            var ca = new Vec3(c.X - a.X, c.Y - a.Y, c.Z - a.Z);
            var cross = Vec3.Cross(ba, ca);

            return Vec3.Dot(normal, cross) > 0;

        }

        private Boolean RequiresUint32Indices(ObjModel objModel)
        {
            return objModel.Vertices.Count > 65534;
        }

        #region Buffers

        private List<Byte> GltfToGlb(GltfModel gltfModel, List<Byte> binaryBuffer)
        {
            var buffer = gltfModel.Buffers[0];
            if (!String.IsNullOrEmpty(buffer.Uri))
            {
                binaryBuffer = new List<Byte>();
            }
            var jsonBuffer = GetJsonBufferPadded(gltfModel);
            // Allocate buffer (Global header) + (JSON chunk header) + (JSON chunk) + (Binary chunk header) + (Binary chunk)
            var glbLength = 12 + 8 + jsonBuffer.Length + 8 + binaryBuffer.Count;

            var glb = new List<Byte>(glbLength);

            // Write binary glTF header (magic, version, length)
            var byteOffset = 0;
            glb.AddRange(BitConverter.GetBytes((UInt32)0x46546C67));
            byteOffset += 4;
            glb.AddRange(BitConverter.GetBytes((UInt32)2));
            byteOffset += 4;
            glb.AddRange(BitConverter.GetBytes((UInt32)glbLength));
            byteOffset += 4;

            // Write JSON Chunk header (length, type)
            glb.AddRange(BitConverter.GetBytes((UInt32)jsonBuffer.Length));
            byteOffset += 4;
            glb.AddRange(BitConverter.GetBytes((UInt32)0x4E4F534A)); // Json
            byteOffset += 4;
            // Write JSON Chunk
            glb.AddRange(jsonBuffer);
            byteOffset += jsonBuffer.Length;

            // Write Binary Chunk header (length, type)
            glb.AddRange(BitConverter.GetBytes((UInt32)binaryBuffer.Count));
            byteOffset += 4;
            glb.AddRange(BitConverter.GetBytes((UInt32)0x004E4942)); // BIN
                                                                     // Write Binary Chunk
            glb.AddRange(binaryBuffer);

            return glb;
        }

        /// <summary>
        /// padding json buffer
        /// </summary>
        /// <param name="model"></param>
        /// <param name="boundary"></param>
        /// <param name="offset">The byte offset on which the buffer starts.</param>
        /// <returns></returns>
        public static Byte[] GetJsonBufferPadded(Object model, Int32 boundary = 4, Int32 offset = 0)
        {
            var json = ToJson(model);
            var bs = Encoding.UTF8.GetBytes(json);
            var remainder = (offset + bs.Length) % boundary;
            var padding = (remainder == 0) ? 0 : boundary - remainder;
            for (var i = 0; i < padding; i++)
            {
                json += " ";
            }
            return Encoding.UTF8.GetBytes(json);
        }

        private Int32 AddIndexArray(GltfModel gltfModel, Int32[] indices, Boolean u32IndicesEnabled, String name)
        {
            var cType = u32IndicesEnabled ? ComponentType.U32 : ComponentType.U16;

            var count = indices.Length;
            var minMax = new DoubleRange();
            UpdateMinMax(indices.Select(c => (Double)c).ToArray(), minMax);

            var accessor = new Accessor
            {
                Type = AccessorType.SCALAR,
                ComponentType = cType,
                Count = count,
                Min = new[] { Math.Round(minMax.Min) },
                Max = new[] { Math.Round(minMax.Max) },
                Name = name
            };

            var index = gltfModel.Accessors.Count;
            gltfModel.Accessors.Add(accessor);
            return index;
        }

        private static Byte[] ToU32Buffer(Int32[] arr)
        {
            var bytes = new List<Byte>();
            foreach (var i in arr)
            {
                bytes.AddRange(BitConverter.GetBytes((UInt32)i));
            }
            return bytes.ToArray();
        }

        private static Byte[] ToU16Buffer(Int32[] arr)
        {
            var bytes = new List<Byte>();
            foreach (var i in arr)
            {
                bytes.AddRange(BitConverter.GetBytes((UInt16)i));
            }
            return bytes.ToArray();
        }
        /// <summary>
        /// padding buffers with boundary
        /// </summary>
        /// <param name="buffers"></param>
        /// <param name="boundary"></param>
        public static void PaddingBuffers(List<Byte> buffers, Int32 boundary = 4)
        {
            var length = buffers.Count;
            var remainder = length % boundary;
            if (remainder != 0)
            {
                var padding = boundary - remainder;
                for (var i = 0; i < padding; i++)
                {
                    buffers.Add(0);
                }
            }
        }

        private List<Byte> AddBuffers(GltfModel gltfModel, BufferState bufferState, GltfOptions options)
        {
            AddBufferView(gltfModel, bufferState.PositionBuffers, bufferState.PositionAccessors.ToArray(), 12, 0x8892);
            AddBufferView(gltfModel, bufferState.NormalBuffers, bufferState.NormalAccessors.ToArray(), 12, 0x8892);
            AddBufferView(gltfModel, bufferState.UvBuffers, bufferState.UvAccessors.ToArray(), 8, 0x8892); // ARRAY_BUFFER
            AddBufferView(gltfModel, bufferState.IndexBuffers, bufferState.IndexAccessors.ToArray(), null, 0x8893); // ELEMENT_ARRAY_BUFFER
            if (options.WithBatchTable)
            {
                AddBufferView(gltfModel, bufferState.BatchIdBuffers, bufferState.BatchIdAccessors.ToArray(), 0, 0x8892);
            }

            var buffers = new List<Byte>();
            foreach (var b in bufferState.PositionBuffers)
            {
                buffers.AddRange(b);
            }
            foreach (var b in bufferState.NormalBuffers)
            {
                buffers.AddRange(b);
            }
            foreach (var b in bufferState.UvBuffers)
            {
                buffers.AddRange(b);
            }
            foreach (var b in bufferState.IndexBuffers)
            {
                buffers.AddRange(b);
            }
            if (options.WithBatchTable)
            {
                foreach (var b in bufferState.BatchIdBuffers)
                {
                    buffers.AddRange(b);
                }
            }
            PaddingBuffers(buffers);
            return buffers;
        }

        private void AddBufferView(GltfModel gltfModel,
                                   List<Byte[]> buffers,
                                   Int32[] accessors,
                                   Int32? byteStride,
                                   Int32? target)
        {
            if (buffers.Count == 0) return;

            BufferView previousBufferView = null;
            if (gltfModel.BufferViews.Count > 0)
            {
                previousBufferView = gltfModel.BufferViews[gltfModel.BufferViews.Count - 1];
            }
            var byteOffset = previousBufferView != null ?
                previousBufferView.ByteOffset + previousBufferView.ByteLength : 0;
            var byteLength = 0;
            var bufferViewIndex = gltfModel.BufferViews.Count;
            for (var i = 0; i < buffers.Count; i++)
            {
                var accessor = gltfModel.Accessors[accessors[i]];
                accessor.BufferView = bufferViewIndex;
                accessor.ByteOffset = byteLength;
                byteLength += buffers[i].Length;
            }
            var bf = new BufferView
            {
                Name = "bufferView_" + bufferViewIndex,
                Buffer = 0,
                ByteLength = byteLength,
                ByteOffset = byteOffset,
                ByteStride = byteStride,
                Target = target
            };
            gltfModel.BufferViews.Add(bf);
        }

        private void FillImageBuffers(GltfModel gltfModel, String objFolder, List<Byte> buffers, Int32 boundary)
        {
            var bufferViewIndex = gltfModel.BufferViews.Count;
            var byteOffset = buffers.Count;
            foreach (var img in gltfModel.Images)
            {
                var imageFile = Path.Combine(objFolder, img.Name);
                var textureSource = File.ReadAllBytes(imageFile);
                var textureByteLength = textureSource.Length;
                img.BufferView = gltfModel.BufferViews.Count;
                gltfModel.BufferViews.Add(new BufferView
                {
                    Buffer = 0,
                    ByteOffset = byteOffset,
                    ByteLength = textureByteLength
                });
                byteOffset += textureByteLength;
                buffers.AddRange(textureSource);
            }
            // Padding Buffers
            PaddingBuffers(buffers);
            gltfModel.Buffers[0].ByteLength = buffers.Count;
        }

        #endregion Buffers

        #region Materials

        /// <summary>
        /// Translate the blinn-phong model to the pbr metallic-roughness model
        /// Roughness factor is a combination of specular intensity and shininess
        /// Metallic factor is 0.0
        /// Textures are not converted for now
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Double Luminance(FactorColor color)
        {
            return color.Red * 0.2125 + color.Green * 0.7154 + color.Blue * 0.0721;
        }

        private Int32 AddTexture(GltfModel gltfModel, String textureFilename)
        {
            var image = new Image
            {
                Name = textureFilename,
                BufferView = 0
            };
            var ext = Path.GetExtension(textureFilename).ToUpper();
            switch (ext)
            {
                case ".PNG":
                    image.MimeType = "image/png";
                    break;
                case ".JPEG":
                case ".JPG":
                    image.MimeType = "image/jpeg";
                    break;
                case ".GIF":
                    image.MimeType = "image/gif";
                    break;
            }
            var imageIndex = gltfModel.Images.Count;
            gltfModel.Images.Add(image);

            var textureIndex = gltfModel.Textures.Count;
            var t = new Gltf.Texture
            {
                Name = textureFilename,
                Source = imageIndex,
                Sampler = 0
            };
            gltfModel.Textures.Add(t);
            return textureIndex;
        }

        private Gltf.Material GetDefault(String name = "default", AlphaMode mode = AlphaMode.OPAQUE)
        {
            return new Gltf.Material
            {
                AlphaMode = mode,
                Name = name,
                //EmissiveFactor = new double[] { 1, 1, 1 },
                PbrMetallicRoughness = new PbrMetallicRoughness
                {
                    BaseColorFactor = new Double[] { 0.5, 0.5, 0.5, 1 },
                    MetallicFactor = 1.0,
                    RoughnessFactor = 0.0
                }
            };
        }

        private static Double Clamp(Double val, Double min, Double max)
        {
            if (val < min) return min;
            if (val > max) return max;
            return val;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mat"></param>
        /// <returns>roughnessFactor</returns>
        private static Double ConvertTraditional2MetallicRoughness(WaveFront.Material mat)
        {
            // Transform from 0-1000 range to 0-1 range. Then invert.
            //var roughnessFactor = mat.SpecularExponent; // options.metallicRoughness ? 1.0 : 0.0;
            //roughnessFactor = roughnessFactor / 1000.0;
            var roughnessFactor = 1.0 - mat.SpecularExponent / 1000.0;
            roughnessFactor = Clamp(roughnessFactor, 0.0, 1.0);

            if (mat.Specular == null || mat.Specular.Color == null)
            {
                mat.Specular = new Reflectivity(new FactorColor());
                return roughnessFactor;
            }
            // Translate the blinn-phong model to the pbr metallic-roughness model
            // Roughness factor is a combination of specular intensity and shininess
            // Metallic factor is 0.0
            // Textures are not converted for now
            var specularIntensity = Luminance(mat.Specular.Color);


            // Low specular intensity values should produce a rough material even if shininess is high.
            if (specularIntensity < 0.1)
            {
                roughnessFactor *= (1.0 - specularIntensity);
            }

            var metallicFactor = 0.0;
            mat.Specular = new Reflectivity(new FactorColor(metallicFactor));
            return roughnessFactor;
        }

        private Int32 AddMaterial(GltfModel gltfModel, Gltf.Material material)
        {
            if (material == null) throw new ArgumentNullException(nameof(material));
            var matIndex = gltfModel.Materials.Count;
            gltfModel.Materials.Add(material);
            return matIndex;
        }

        Int32 GetTextureIndex(GltfModel gltfModel, String path)
        {
            for (var i = 0; i < gltfModel.Textures.Count; i++)
            {
                if (path == gltfModel.Textures[i].Name)
                {
                    return i;
                }
            }
            return AddTexture(gltfModel, path);
        }

        public static Gltf.Material ConvertMaterial(GltfModel gltfModel, WaveFront.Material mat, GetOrAddTexture getOrAddTextureFunction)
        {
            var roughnessFactor = ConvertTraditional2MetallicRoughness(mat);

            var gMat = new Gltf.Material
            {
                Name = mat.Name,
                AlphaMode = AlphaMode.OPAQUE
            };

            var alpha = mat.GetAlpha();
            var metallicFactor = 0.0;
            if (mat.Specular != null && mat.Specular.Color != null)
            {
                metallicFactor = mat.Specular.Color.Red;
            }
            gMat.PbrMetallicRoughness = new PbrMetallicRoughness
            {
                RoughnessFactor = roughnessFactor,
                MetallicFactor = metallicFactor
            };
            if (mat.Diffuse != null)
            {
                gMat.PbrMetallicRoughness.BaseColorFactor = mat.Diffuse.Color.ToArray(alpha);
            }
            else if (mat.Ambient != null)
            {
                gMat.PbrMetallicRoughness.BaseColorFactor = mat.Ambient.Color.ToArray(alpha);
            }
            else
            {
                gMat.PbrMetallicRoughness.BaseColorFactor = new Double[] { 0.7, 0.7, 0.7, alpha };
            }


            var hasTexture = !String.IsNullOrEmpty(mat.DiffuseTextureFile);
            if (hasTexture)
            {
                var index = getOrAddTextureFunction(gltfModel, mat.DiffuseTextureFile);
                gMat.PbrMetallicRoughness.BaseColorTexture = new TextureReferenceInfo
                {
                    Index = index
                };
            }


            if (mat.Emissive != null && mat.Emissive.Color != null)
            {
                gMat.EmissiveFactor = mat.Emissive.Color.ToArray();
            }

            if (alpha < 1.0)
            {
                gMat.AlphaMode = AlphaMode.BLEND;
                gMat.DoubleSided = true;
            }

            return gMat;
        }

        //TODO: move to gltf model ?!?
        private Int32 GetMaterialIndex(GltfModel gltfModel, String matName)
        {
            for (var i = 0; i < gltfModel.Materials.Count; i++)
            {
                if (gltfModel.Materials[i].Name == matName)
                {
                    return i;
                }
            }
            return -1;
        }

        #endregion Materials

        #region Meshes

        private Int32 AddMesh(GltfModel gltfModel, ObjModel objModel, BufferState buffer, Geometry mesh, Boolean uint32Indices, GltfOptions options)
        {
            var ps = AddVertexAttributes(gltfModel, objModel, buffer, mesh, uint32Indices, options);

            var m = new Mesh
            {
                Name = mesh.Id,
                Primitives = ps
            };
            var meshIndex = gltfModel.Meshes.Count;
            gltfModel.Meshes.Add(m);
            return meshIndex;

        }

        /// <summary>
        /// update bounding box with double array
        /// </summary>
        /// <param name="vs"></param>
        /// <param name="minMax"></param>

        public static void UpdateMinMax(Double[] vs, DoubleRange minMax)
        {
            var min = vs.Min();
            var max = vs.Max();
            if (minMax.Min > min)
            {
                minMax.Min = min;
            }
            if (minMax.Max < max)
            {
                minMax.Max = max;
            }
        }



        private List<Primitive> AddVertexAttributes(GltfModel gltfModel,
                                                    ObjModel objModel,
                                                    BufferState buffers,
                                                    Geometry mesh,
                                                    Boolean uint32Indices,
                                                    GltfOptions options)
        {
            var facesGroup = mesh.Faces.GroupBy(c => c.MatName);
            var faces = new List<Face>();
            foreach (var fg in facesGroup)
            {
                var matName = fg.Key;
                var f = new Face { MatName = matName };
                foreach (var ff in fg)
                {
                    f.Triangles.AddRange(ff.Triangles);
                }
                if (f.Triangles.Count > 0)
                {
                    faces.Add(f);
                }
            }

            var hasPositions = faces.Count > 0;
            var hasUvs = faces.Any(c => c.Triangles.Any(d => d.V1.T > 0));
            var hasNormals = faces.Any(c => c.Triangles.Any(d => d.V1.N > 0));

            var vertices = objModel.Vertices;
            var normals = objModel.Normals;
            var uvs = objModel.Uvs;

            // Vertex attributes are shared by all primitives in the mesh
            var name0 = mesh.Id;

            var ps = new List<Primitive>(faces.Count * 2);
            var index = 0;
            foreach (var f in faces)
            {
                var faceName = name0;
                if (index > 0)
                {
                    faceName = name0 + "_" + index;
                }
                DoubleRange vmmX = new DoubleRange(), vmmY = new DoubleRange(), vmmZ = new DoubleRange();
                DoubleRange nmmX = new DoubleRange(), nmmY = new DoubleRange(), nmmZ = new DoubleRange();
                DoubleRange tmmX = new DoubleRange(), tmmY = new DoubleRange();
                var vList = 0;
                var nList = 0;
                var tList = 0;
                var vs = new List<Byte>(); // vertexBuffers
                var ns = new List<Byte>(); // normalBuffers
                var ts = new List<Byte>(); // textureBuffers

                // every primitive need their own vertex indices(v,t,n)
                var FaceVertexCache = new Dictionary<String, Int32>();
                var FaceVertexCount = 0;

                //List<int[]> indiceList = new List<int[]>(faces.Count * 2);
                //var matIndexList = new List<int>(faces.Count * 2);

                // f is a primitive
                var iList = new List<Int32>(f.Triangles.Count * 3 * 2); // primitive indices
                foreach (var t in f.Triangles)
                {
                    var v1Index = t.V1.V - 1;
                    var v2Index = t.V2.V - 1;
                    var v3Index = t.V3.V - 1;
                    var v1 = vertices[v1Index];
                    var v2 = vertices[v2Index];
                    var v3 = vertices[v3Index];
                    UpdateMinMax(new[] { v1.X, v2.X, v3.X }, vmmX);
                    UpdateMinMax(new[] { v1.Y, v2.Y, v3.Y }, vmmY);
                    UpdateMinMax(new[] { v1.Z, v2.Z, v3.Z }, vmmZ);

                    Vec3 n1 = new Vec3(), n2 = new Vec3(), n3 = new Vec3();
                    if (t.V1.N > 0) // hasNormals
                    {
                        var n1Index = t.V1.N - 1;
                        var n2Index = t.V2.N - 1;
                        var n3Index = t.V3.N - 1;
                        n1 = normals[n1Index];
                        n2 = normals[n2Index];
                        n3 = normals[n3Index];
                        UpdateMinMax(new[] { n1.X, n2.X, n3.X }, nmmX);
                        UpdateMinMax(new[] { n1.Y, n2.Y, n3.Y }, nmmY);
                        UpdateMinMax(new[] { n1.Z, n2.Z, n3.Z }, nmmZ);
                    }
                    Vec2 t1 = new Vec2(), t2 = new Vec2(), t3 = new Vec2();
                    if (t.V1.T > 0) // hasUvs
                    {
                        var t1Index = t.V1.T - 1;
                        var t2Index = t.V2.T - 1;
                        var t3Index = t.V3.T - 1;
                        t1 = uvs[t1Index];
                        t2 = uvs[t2Index];
                        t3 = uvs[t3Index];
                        UpdateMinMax(new[] { t1.U, t2.U, t3.U }, tmmX);
                        UpdateMinMax(new[] { 1 - t1.V, 1 - t2.V, 1 - t3.V }, tmmY);
                    }


                    var v1Str = t.V1.ToString();
                    if (!FaceVertexCache.ContainsKey(v1Str))
                    {
                        FaceVertexCache.Add(v1Str, FaceVertexCount++);

                        vList++; vs.AddRange(v1.ToFloatBytes());
                        if (t.V1.N > 0) // hasNormals
                        {
                            nList++; ns.AddRange(n1.ToFloatBytes());
                        }
                        if (t.V1.T > 0) // hasUvs
                        {
                            tList++; ts.AddRange(new Vec2(t1.U, 1 - t1.V).ToFloatBytes());
                        }

                    }

                    var v2Str = t.V2.ToString();
                    if (!FaceVertexCache.ContainsKey(v2Str))
                    {
                        FaceVertexCache.Add(v2Str, FaceVertexCount++);

                        vList++; vs.AddRange(v2.ToFloatBytes());
                        if (t.V2.N > 0) // hasNormals
                        {
                            nList++; ns.AddRange(n2.ToFloatBytes());
                        }
                        if (t.V2.T > 0) // hasUvs
                        {
                            tList++; ts.AddRange(new Vec2(t2.U, 1 - t2.V).ToFloatBytes());
                        }

                    }

                    var v3Str = t.V3.ToString();
                    if (!FaceVertexCache.ContainsKey(v3Str))
                    {
                        FaceVertexCache.Add(v3Str, FaceVertexCount++);

                        vList++; vs.AddRange(v3.ToFloatBytes());
                        if (t.V3.N > 0) // hasNormals
                        {
                            nList++; ns.AddRange(n3.ToFloatBytes());
                        }
                        if (t.V3.T > 0) // hasUvs
                        {
                            tList++; ts.AddRange(new Vec2(t3.U, 1 - t3.V).ToFloatBytes());
                        }

                    }

                    // Vertex Indices
                    var correctWinding = CheckWindingCorrect(v1, v2, v3, n1);
                    if (correctWinding)
                    {
                        iList.AddRange(new[] {
                            FaceVertexCache[v1Str],
                            FaceVertexCache[v2Str],
                            FaceVertexCache[v3Str]
                        });
                    }
                    else
                    {
                        iList.AddRange(new[] {
                            FaceVertexCache[v1Str],
                            FaceVertexCache[v3Str],
                            FaceVertexCache[v2Str]
                        });
                    }

                }

                var materialIndex = GetMaterialIndexOrDefault(gltfModel, objModel, f.MatName);

                var atts = new Dictionary<String, Int32>();

                var accessorIndex = gltfModel.Accessors.Count;
                var accessorVertex = new Accessor
                {
                    Min = new Double[] { vmmX.Min, vmmY.Min, vmmZ.Min },
                    Max = new Double[] { vmmX.Max, vmmY.Max, vmmZ.Max },
                    Type = AccessorType.VEC3,
                    Count = vList,
                    ComponentType = ComponentType.F32,
                    Name = faceName + "_positions"
                };
                gltfModel.Accessors.Add(accessorVertex);
                atts.Add("POSITION", accessorIndex);
                buffers.PositionBuffers.Add(vs.ToArray());
                buffers.PositionAccessors.Add(accessorIndex);

                if (options.WithBatchTable)
                {
                    buffers.BatchTableJson.MaxPoint.Add(accessorVertex.Max);
                    buffers.BatchTableJson.MinPoint.Add(accessorVertex.Min);
                }

                if (nList > 0) //hasNormals)
                {
                    accessorIndex = gltfModel.Accessors.Count;
                    var accessorNormal = new Accessor
                    {
                        Min = new Double[] { nmmX.Min, nmmY.Min, nmmZ.Min },
                        Max = new Double[] { nmmX.Max, nmmY.Max, nmmZ.Max },
                        Type = AccessorType.VEC3,
                        Count = nList,
                        ComponentType = ComponentType.F32,
                        Name = faceName + "_normals"
                    };
                    gltfModel.Accessors.Add(accessorNormal);
                    atts.Add("NORMAL", accessorIndex);
                    buffers.NormalBuffers.Add(ns.ToArray());
                    buffers.NormalAccessors.Add(accessorIndex);
                }

                if (tList > 0) //hasUvs)
                {
                    accessorIndex = gltfModel.Accessors.Count;
                    var accessorUv = new Accessor
                    {
                        Min = new Double[] { tmmX.Min, tmmY.Min },
                        Max = new Double[] { tmmX.Max, tmmY.Max },
                        Type = AccessorType.VEC2,
                        Count = tList,
                        ComponentType = ComponentType.F32,
                        Name = faceName + "_texcoords"
                    };
                    gltfModel.Accessors.Add(accessorUv);
                    atts.Add("TEXCOORD_0", accessorIndex);
                    buffers.UvBuffers.Add(ts.ToArray());
                    buffers.UvAccessors.Add(accessorIndex);
                }
                else
                {
                    var gMat = gltfModel.Materials[materialIndex];
                    if (gMat.PbrMetallicRoughness.BaseColorTexture != null)
                    {
                        gMat.PbrMetallicRoughness.BaseColorTexture = null;
                    }
                }


                if (options.WithBatchTable)
                {
                    var batchIdCount = vList;
                    accessorIndex = AddBatchIdAttribute(
                        gltfModel,
                        buffers.CurrentBatchId,
                        batchIdCount, faceName + "_batchId");
                    atts.Add("_BATCHID", accessorIndex);
                    var batchIds = new List<Byte>();
                    for (var i = 0; i < batchIdCount; i++)
                    {
                        batchIds.AddRange(BitConverter.GetBytes((UInt16)buffers.CurrentBatchId));
                    }
                    buffers.BatchIdBuffers.Add(batchIds.ToArray());
                    buffers.BatchIdAccessors.Add(accessorIndex);
                    buffers.BatchTableJson.BatchIds.Add((UInt16)buffers.CurrentBatchId);
                    buffers.BatchTableJson.Names.Add(faceName);
                    buffers.CurrentBatchId++;
                }


                var indices = iList.ToArray();
                var indexAccessorIndex = AddIndexArray(gltfModel, indices, uint32Indices, faceName + "_indices");
                var indexBuffer = uint32Indices ? ToU32Buffer(indices) : ToU16Buffer(indices);
                buffers.IndexBuffers.Add(indexBuffer);
                buffers.IndexAccessors.Add(indexAccessorIndex);

                var p = new Primitive
                {
                    Attributes = atts,
                    Indices = indexAccessorIndex,
                    Material = materialIndex,//matIndexList[i],
                    Mode = MeshMode.Triangles
                };
                ps.Add(p);


                index++;
            }

            return ps;
        }

        private Int32 GetMaterialIndexOrDefault(GltfModel gltfModel, ObjModel objModel, String materialName)
        {
            if (String.IsNullOrEmpty(materialName)) materialName = "default";

            var materialIndex = GetMaterialIndex(gltfModel, materialName);
            if (materialIndex == -1)
            {
                var objMaterial = objModel.Materials.FirstOrDefault(c => c.Name == materialName);
                if (objMaterial == null)
                {
                    materialName = "default";
                    materialIndex = GetMaterialIndex(gltfModel, materialName);
                    if (materialIndex == -1)
                    {
                        var gMat = GetDefault();
                        materialIndex = AddMaterial(gltfModel, gMat);
                    }
                    else
                    {
#if DEBUG
                        System.Diagnostics.Debugger.Break();
#endif
                    }
                }
                else
                {
                    var gMat = ConvertMaterial(gltfModel, objMaterial, GetTextureIndex);
                    materialIndex = AddMaterial(gltfModel, gMat);
                }
            }

            return materialIndex;
        }

        private Int32 AddBatchIdAttribute(GltfModel gltfModel, Int32 batchId, Int32 count, String name)
        {
            //var ctype = u32IndicesEnabled ? ComponentType.U32 : ComponentType.U16;
            var ctype = ComponentType.U16;
            var accessor = new Accessor
            {
                Name = name,
                ComponentType = ctype,
                Count = count,
                Min = new Double[] { batchId },
                Max = new Double[] { batchId },
                Type = AccessorType.SCALAR
            };
            var accessorIndex = gltfModel.Accessors.Count;
            gltfModel.Accessors.Add(accessor);
            return accessorIndex;
        }

        private Int32 AddNode(GltfModel gltfModel, String name, Int32? meshIndex, Int32? parentIndex = null)
        {
            var node = new Node { Name = name, Mesh = meshIndex };
            var nodeIndex = gltfModel.Nodes.Count;
            gltfModel.Nodes.Add(node);
            //if (parentIndex != null)
            //{
            //    var pNode = _model.Nodes[parentIndex.Value];
            //    //TODO:
            //}
            //else
            //{

            //}
            gltfModel.Scenes[gltfModel.Scene].Nodes.Add(nodeIndex);

            return nodeIndex;
        }

        #endregion Meshes
    }
}
