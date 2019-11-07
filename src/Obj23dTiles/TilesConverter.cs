using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Arctron.Obj2Gltf;
using Newtonsoft.Json;
using Arctron.Obj2Gltf.WaveFront;

namespace Arctron.Obj23dTiles
{

    public class TilesOptions
    {
        /// <summary>
        /// whether to merge tileset.json files. 
        /// if true, only one tileset.json file will be generated, 
        /// all the b3dm files will be root's first level children
        /// </summary>
        public Boolean MergeTileJsonFiles { get; set; } = true;
        /// <summary>
        /// keep children b3dm tileset.json
        /// </summary>
        public Boolean WriteChildTileJson { get; set; } = true;
        /// <summary>
        /// converted tiles folder
        /// </summary>
        public String OutputFolder { get; set; }
    }
    /// <summary>
    /// tileset converter
    /// </summary>
    public class TilesConverter
    {
        private readonly String _objFolder;
        private readonly List<ObjModel> _objModels;
        private readonly GisPosition _gisPosition;

        private readonly TilesOptions _options;
        /// <summary>
        /// whether to merge tileset.json files. 
        /// if true, only one tileset.json file will be generated, 
        /// all the b3dm files will be root's first level children
        /// </summary>
        [Obsolete("Please use TilesOptions.MergeTileJsonFiles")]
        public Boolean MergeTileJsonFiles { get; set; } = true;
        /// <summary>
        /// keep children b3dm tileset.json
        /// </summary>
        [Obsolete("Please use TilesOptions.WriteChildTileJson")]
        public Boolean WriteChildTileJson { get; set; } = true;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objFolder">folder contains obj files</param>
        /// <param name="outputFolder">tiles output folder</param>
        /// <param name="gisPosition">where the tiles are</param>        
        public TilesConverter(String objFolder, String outputFolder,
            GisPosition gisPosition)
        {
            _objFolder = objFolder;
            _gisPosition = gisPosition;
            _options = new TilesOptions { OutputFolder = outputFolder };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="objFolder">all objModels should be in this folder</param>
        /// <param name="objModels"></param>
        /// <param name="gisPosition"></param>
        /// <param name="options"></param>
        public TilesConverter(String objFolder, ICollection<ObjModel> objModels, GisPosition gisPosition, TilesOptions options)
        {
            _objFolder = objFolder;
            _gisPosition = gisPosition;
            if (objModels is List<ObjModel>)
            {
                _objModels = (List<ObjModel>)objModels;
            }
            else
            {
                _objModels = new List<ObjModel>(objModels);
            }
            _options = options ?? new TilesOptions();
            if (String.IsNullOrEmpty(_options.OutputFolder))
            {
                _options.OutputFolder = Path.Combine(objFolder, "ConvertedTiles");
            }
            if (_options != null)
            {
                MergeTileJsonFiles = _options.MergeTileJsonFiles;
                WriteChildTileJson = _options.WriteChildTileJson;
            }
        }
        /// <summary>
        /// run converter
        /// </summary>
        /// <param name="lod">whether generate hierarchical tileset.json</param>
        /// <returns></returns>
        public String Run(Boolean lod = false)
        {
            var outputDir = _options.OutputFolder;
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            var objFolder = _objFolder;
            // 31.19703,121.45238
            var gisPosition = _gisPosition;
            if (_objModels == null)
            {
                var objFiles = Directory.GetFiles(objFolder, "*.obj");
                if (MergeTileJsonFiles)
                {
                    return MergeTilesets(outputDir, gisPosition, lod, WriteChildTileJson, objFiles);
                }
                return CombineTilesets(outputDir, gisPosition, objFiles);
            }
            else
            {
                if (MergeTileJsonFiles)
                {
                    return MergeTilesets(objFolder, outputDir, gisPosition, lod, WriteChildTileJson, _objModels.ToArray());
                }
                return CombineTilesets(objFolder, outputDir, gisPosition, _objModels.ToArray());
            }

        }

        internal static String MergeTilesets(String outputFolder, GisPosition gisPosition, Boolean lod, Boolean writeChildTilesetJson, params Task<SingleTileset>[] waitedTasks)
        {
            var west = Double.MaxValue;
            var south = Double.MaxValue;
            var north = Double.MinValue;
            var east = Double.MinValue;
            var minheight = Double.MaxValue;
            var maxheight = Double.MinValue;

            var outputTileset = Path.Combine(outputFolder, "tileset.json");
            var outputPath = GenerateBatchedPath(outputFolder);

            var tilesetList = new List<SingleTileset>();

            var tiles = new List<Tile>();
            var geometricError = 500.0;
            foreach (var t in waitedTasks)
            {
                var json = t.Result;
                tilesetList.Add(json);
                if (json.Root == null) continue;
                var boundingVolume = json.Root.BoundingVolume;
                if (boundingVolume == null) continue;
                var geometricError0 = json.GeometricError;
                if (boundingVolume.Region != null
                    && boundingVolume.Region.Length >= 6)
                {
                    west = Math.Min(west, boundingVolume.Region[0]);
                    south = Math.Min(south, boundingVolume.Region[1]);
                    east = Math.Max(east, boundingVolume.Region[2]);
                    north = Math.Max(north, boundingVolume.Region[3]);
                    minheight = Math.Min(minheight, boundingVolume.Region[4]);
                    maxheight = Math.Max(maxheight, boundingVolume.Region[5]);
                }
                var contentName = Path.GetFileNameWithoutExtension(json.Root.Content.Url);
                if (!lod)
                {
                    var err = geometricError0 / 4.0;
                    if (json.Root != null)
                    {
                        Double? err0 = null;
                        if (json.Root.OriginalX != null && json.Root.OriginalX.IsValid())
                        {
                            var errX = (json.Root.OriginalX.Max - json.Root.OriginalX.Min) / 10;
                            err0 = errX;
                        }
                        if (json.Root.OriginalY != null && json.Root.OriginalY.IsValid())
                        {
                            var errY = (json.Root.OriginalY.Max - json.Root.OriginalY.Min) / 10;
                            if (!err0.HasValue)
                            {
                                err0 = errY;
                            }
                            else if (err0.Value < errY)
                            {
                                err0 = errY;
                            }
                        }
                        if (json.Root.OriginalZ != null && json.Root.OriginalZ.IsValid())
                        {
                            var errZ = (json.Root.OriginalZ.Max - json.Root.OriginalZ.Min) / 10;
                            if (!err0.HasValue)
                            {
                                err0 = errZ;
                            }
                            else if (err0.Value < errZ)
                            {
                                err0 = errZ;
                            }
                        }
                        if (err0.HasValue)
                        {
                            err = err0.Value;
                        }
                    }
                    tiles.Add(new Tile
                    {
                        BoundingVolume = boundingVolume,
                        GeometricError = 0.0,
                        Refine = null,
                        Content = new TileContent
                        {
                            Url = DataFolderName + "/" + json.Root.Content.Url,
                            BoundingVolume = boundingVolume
                        }
                    });
                }
                if (writeChildTilesetJson)
                {
                    var jsonFilepath = Path.Combine(outputPath, contentName + ".json");
                    WriteTilesetJsonFile(jsonFilepath, json);
                }
            }
            if (lod)
            {
                tiles = TilesetRegionComparer.SortTrees(tilesetList, DataFolderName);
            }

            var longitude = gisPosition.Longitude;
            var latitude = gisPosition.Latitude;
            var transHeight = gisPosition.TransHeight.HasValue ? gisPosition.TransHeight.Value : 0.0;
            var transformArray = GisUtil.Wgs84Transform(
                    longitude, latitude, transHeight).ToArray();

            var allTileset = new SingleTileset
            {
                Asset = new TilesetAsset
                {
                    Version = "0.0",
                    TilesetVersion = "1.0.0-arctron"
                },
                GeometricError = geometricError,
                Root = new Tile
                {
                    BoundingVolume = new BoundingVolume
                    {
                        Region = new Double[] {
                            west,south,
                            east, north,
                            minheight, maxheight }
                    },
                    Refine = "ADD",
                    GeometricError = geometricError / 2.0,
                    Children = tiles,
                    Transform = transformArray
                }

            };


            allTileset.ResetGeometricErrors(); //TODO:

            var tilesetJsonPath = Path.Combine(outputFolder, "tileset.json");
            File.WriteAllText(tilesetJsonPath,
                JsonConvert.SerializeObject(allTileset,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                    //,Formatting = Formatting.Indented
                }));
            return tilesetJsonPath;
        }

        private static readonly String DataFolderName = "BatchedModels";

        private static String GenerateBatchedPath(String outputFolder)
        {
            var outputPath = Path.Combine(outputFolder, DataFolderName);
            return outputPath;
        }
        /// <summary>
        /// when converted, merge tileset.json
        /// </summary>
        /// <param name="outputFolder">tiles output folder</param>
        /// <param name="gisPosition">where the tiles are</param>
        /// <param name="lod">whether generate hierarchical tileset.json</param>
        /// <param name="writeChildTilesetJson">keep children b3dm tileset.json</param>
        /// <param name="objModels">obj file list</param>
        /// <returns></returns>
        internal static String MergeTilesets(String objFolder, String outputFolder,
            GisPosition gisPosition, Boolean lod, Boolean writeChildTilesetJson, params ObjModel[] objModels)
        {
            var tasks = new Task<SingleTileset>[objModels.Length];
            var outputPath = GenerateBatchedPath(outputFolder);
            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);
            for (var i = 0; i < objModels.Length; i++)
            {
                var objFile = objModels[i];
                tasks[i] = Task.Run(() => WriteTileset(objFile, objFolder, outputPath, gisPosition));
            }
            Task.WaitAll(tasks);

            return MergeTilesets(outputFolder, gisPosition, lod, writeChildTilesetJson, tasks);
        }
        /// <summary>
        /// when converted, merge tileset.json
        /// </summary>
        /// <param name="outputFolder">tiles output folder</param>
        /// <param name="gisPosition">where the tiles are</param>
        /// <param name="lod">whether generate hierarchical tileset.json</param>
        /// <param name="writeChildTilesetJson">keep children b3dm tileset.json</param>
        /// <param name="objFiles">obj file list</param>
        /// <returns></returns>
        internal static String MergeTilesets(String outputFolder, GisPosition gisPosition, Boolean lod,
            Boolean writeChildTilesetJson, params String[] objFiles)
        {
            var tasks = new Task<SingleTileset>[objFiles.Length];
            var outputPath = GenerateBatchedPath(outputFolder);
            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);
            for (var i = 0; i < objFiles.Length; i++)
            {
                var objFile = objFiles[i];
                var name = Path.GetFileNameWithoutExtension(objFile);
                tasks[i] = Task.Run(() => WriteTileset(objFile, outputPath, gisPosition));
            }
            Task.WaitAll(tasks);

            return MergeTilesets(outputFolder, gisPosition, lod, writeChildTilesetJson, tasks);
        }

        internal static String CombineTilesets(String outputFolder, GisPosition gisPosition,
            params Task<String>[] waitedTasks)
        {
            var west = Double.MaxValue;
            var south = Double.MaxValue;
            var north = Double.MinValue;
            var east = Double.MinValue;
            var minheight = Double.MaxValue;
            var maxheight = Double.MinValue;

            var outputTileset = Path.Combine(outputFolder, "tileset.json");

            var tiles = new List<Tile>();
            var geometricError = 500.0;
            foreach (var t in waitedTasks)
            {
                var jsonFile = t.Result;
                var json = JsonConvert.DeserializeObject<SingleTileset>(
                    File.ReadAllText(jsonFile, Encoding.UTF8));
                if (json.Root == null) continue;
                var boundingVolume = json.Root.BoundingVolume;
                if (boundingVolume == null) continue;
                var geometricError0 = json.GeometricError;
                if (boundingVolume.Region != null
                    && boundingVolume.Region.Length >= 6)
                {
                    west = Math.Min(west, boundingVolume.Region[0]);
                    south = Math.Min(south, boundingVolume.Region[1]);
                    east = Math.Max(east, boundingVolume.Region[2]);
                    north = Math.Max(north, boundingVolume.Region[3]);
                    minheight = Math.Min(minheight, boundingVolume.Region[4]);
                    maxheight = Math.Max(maxheight, boundingVolume.Region[5]);
                }

                tiles.Add(new Tile
                {
                    BoundingVolume = boundingVolume,
                    GeometricError = geometricError0,
                    Refine = json.Root.Refine,
                    Content = new TileContent
                    {
                        Url = jsonFile.Substring(outputFolder.Length).TrimStart(new Char[] { '\\', '/' }).Replace('\\', '/')
                    }
                });
            }

            var allTileset = new SingleTileset
            {
                Asset = new TilesetAsset
                {
                    Version = "0.0",
                    TilesetVersion = "1.0.0-arctron"
                },
                GeometricError = geometricError,
                Root = new Tile
                {
                    BoundingVolume = new BoundingVolume
                    {
                        Region = new Double[] {
                            west,south,
                            east, north,
                            minheight, maxheight }
                    },
                    Refine = "ADD",
                    GeometricError = geometricError,
                    Children = tiles
                }

            };
            var tilesetJsonPath = Path.Combine(outputFolder, "tileset.json");
            File.WriteAllText(tilesetJsonPath,
                JsonConvert.SerializeObject(allTileset,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                }));
            return tilesetJsonPath;
        }
        internal static String CombineTilesets(String objFolder, String outputFolder, GisPosition gisPosition,
            params ObjModel[] objModels)
        {
            var tasks = new Task<String>[objModels.Length];
            for (var i = 0; i < objModels.Length; i++)
            {
                var objFile = objModels[i];
                var name = objFile.Name;
                var outputPath = Path.Combine(outputFolder, "Batched" + name);
                if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);
                tasks[i] = Task.Run(() => WriteTilesetFile(objFile, objFolder, outputPath, gisPosition));
            }
            Task.WaitAll(tasks);

            return CombineTilesets(outputFolder, gisPosition, tasks);
        }
        /// <summary>
        /// when converted, combine tileset.json
        /// </summary>
        /// <param name="outputFolder">tiles output folder</param>
        /// <param name="gisPosition">where the tiles are</param>
        /// <param name="objFiles">obj file list</param>
        /// <returns></returns>
        internal static String CombineTilesets(String outputFolder, GisPosition gisPosition,
            params String[] objFiles)
        {
            var tasks = new Task<String>[objFiles.Length];
            for (var i = 0; i < objFiles.Length; i++)
            {
                var objFile = objFiles[i];
                var name = Path.GetFileNameWithoutExtension(objFile);
                var outputPath = Path.Combine(outputFolder, "Batched" + name);
                if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);
                tasks[i] = Task.Run(() => WriteTilesetFile(objFile, outputPath, gisPosition));
            }
            Task.WaitAll(tasks);

            return CombineTilesets(outputFolder, gisPosition, tasks);
        }
        /// <summary>
        /// when converted, generate tileset
        /// </summary>
        /// <param name="objFile">obj file path</param>
        /// <param name="outputPath">output folder</param>
        /// <param name="gisPosition">where the obj model positioned</param>
        /// <returns></returns>
        internal static SingleTileset WriteTileset(String objFile, String outputPath, GisPosition gisPosition)
        {
            var fileName = Path.GetFileNameWithoutExtension(objFile);
            var b3dmFile = Path.Combine(outputPath, fileName + ".b3dm");
            var tilesetOptions = WriteB3dm(objFile, b3dmFile);
            //tilesetOptions.TransHeight = 0;
            //tilesetOptions.MinHeight = 0;
            //tilesetOptions.MaxHeight = 40;
            //tilesetOptions.TileWidth = 200;
            //tilesetOptions.TileHeight = 200;
            tilesetOptions.SetPosition(gisPosition);
            tilesetOptions.UseRegion = true;
            var singleTileset = SingleTileset.Create(tilesetOptions);
            return singleTileset;
        }

        internal static SingleTileset WriteTileset(ObjModel objModel,
            String objFolder, String outputPath, GisPosition gisPosition)
        {
            var modelName = objModel.Name;
            var b3dmFile = Path.Combine(outputPath, modelName + ".b3dm");
            var converter = new Converter(new ObjParser(), new MtlParser());
            converter.Convert(objModel, objFolder, new GltfOptions { Binary = true, Name = modelName, WithBatchTable = true });
            var tilesetOptions = WriteB3dm(converter, b3dmFile);
            //tilesetOptions.TransHeight = 0;
            //tilesetOptions.MinHeight = 0;
            //tilesetOptions.MaxHeight = 40;
            //tilesetOptions.TileWidth = 200;
            //tilesetOptions.TileHeight = 200;
            tilesetOptions.SetPosition(gisPosition);
            tilesetOptions.UseRegion = true;
            var singleTileset = SingleTileset.Create(tilesetOptions);
            return singleTileset;
        }

        private static void WriteTilesetJsonFile(String jsonFilepath, SingleTileset singleTileset)
        {
            var tilesetJson = JsonConvert.SerializeObject(singleTileset, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            });
            File.WriteAllText(jsonFilepath, tilesetJson, Encoding.UTF8);
        }
        /// <summary>
        /// when converted, generate tileset.json
        /// </summary>
        /// <param name="objFile">obj file path</param>
        /// <param name="outputPath">output folder</param>
        /// <param name="gisPosition">where the obj model positioned</param>
        /// <returns></returns>
        public static String WriteTilesetFile(String objFile, String outputPath,
            GisPosition gisPosition)
        {
            var singleTileset = WriteTileset(objFile, outputPath, gisPosition);

            var tilesetFile = Path.Combine(outputPath, "tileset.json");
            WriteTilesetJsonFile(tilesetFile, singleTileset);
            return tilesetFile;
        }

        /// <summary>
        /// when converted, generate tileset.json
        /// </summary>
        /// <param name="objModel">Parsed obj Model</param>
        /// <param name="outputPath">output folder</param>
        /// <param name="gisPosition">where the obj model positioned</param>
        /// <returns></returns>
        public static String WriteTilesetFile(ObjModel objModel,
            String objFolder, String outputPath, GisPosition gisPosition)
        {
            var singleTileset = WriteTileset(objModel, objFolder, outputPath, gisPosition);

            var tilesetFile = Path.Combine(outputPath, "tileset.json");
            WriteTilesetJsonFile(tilesetFile, singleTileset);
            return tilesetFile;
        }

        private static TilesetCreationOptions WriteB3dm(Converter converter,
            String outputFile, Options options = null)
        {
            if (options == null)
            {
                options = new Options();
            }


            var glb = converter.GetGlb();
            var batchTableJson = converter.GetBatchTable();
            var length = batchTableJson.MaxPoint.Count;
            var boundary = 8;
            if (options.FeatureTableJson == null || options.FeatureTableJson.Count == 0)
            {
                if (options.FeatureTableJson == null)
                {
                    options.FeatureTableJson = new List<Byte>();
                }
                var featureTable = new FeatureTable() { BatchLength = (UInt32)length };
                var featureTableJsonBuffer =
                Converter.GetJsonBufferPadded(featureTable, boundary, 28);
                options.FeatureTableJson.AddRange(featureTableJsonBuffer);
            }
            List<Byte> featureTableBinary;
            if (options != null && options.FeatureTableBinary != null)
            {
                featureTableBinary = options.FeatureTableBinary;
            }
            else
            {
                featureTableBinary = new List<Byte>();
            }
            var batchTableJsonBuffer = Converter.GetJsonBufferPadded(batchTableJson, boundary);
            options.BatchTableJson = batchTableJsonBuffer.ToList();
            List<Byte> batchTableBinary;
            if (options != null && options.BatchTableBinary != null)
            {
                batchTableBinary = options.BatchTableBinary;
            }
            else
            {
                batchTableBinary = new List<Byte>();
            }

            var b3dm = new B3dm(glb);
            var bytes = b3dm.Convert(options);

            File.WriteAllBytes(outputFile, bytes);

            var tileFullname = Path.GetFileName(outputFile);
            var folder = Path.GetDirectoryName(outputFile);
            var tilesetPath = Path.Combine(folder, "tileset.json");
            var tilesetOptions = new TilesetCreationOptions();
            //var batchTableJson = json;
            var minMaxX = new DoubleRange();
            Converter.UpdateMinMax(
                batchTableJson.MaxPoint.Select(c => c[0]).Concat(
                    batchTableJson.MinPoint.Select(c => c[0])).ToArray(), minMaxX);
            var minMaxY = new DoubleRange();
            Converter.UpdateMinMax(
                batchTableJson.MaxPoint.Select(c => c[1]).Concat(
                    batchTableJson.MinPoint.Select(c => c[1])).ToArray(), minMaxY);
            var minMaxZ = new DoubleRange();
            Converter.UpdateMinMax(
                batchTableJson.MaxPoint.Select(c => c[2]).Concat(
                    batchTableJson.MinPoint.Select(c => c[2])).ToArray(), minMaxZ);
            var width = minMaxX.Max - minMaxX.Min;
            var height = minMaxZ.Max - minMaxZ.Min;
            width = Math.Ceiling(width);
            height = Math.Ceiling(height);

            var offsetX = width / 2 + minMaxX.Min;
            var offsetY = height / 2 + minMaxZ.Min;
            tilesetOptions.TileName = tileFullname;
            tilesetOptions.TileWidth = width;
            tilesetOptions.TileHeight = height;
            tilesetOptions.OriginalX = minMaxX;
            tilesetOptions.OriginalY = minMaxY;
            tilesetOptions.OriginalZ = minMaxZ;
            tilesetOptions.TransHeight = -minMaxY.Min;
            tilesetOptions.MinHeight = minMaxY.Min + tilesetOptions.TransHeight;
            tilesetOptions.MaxHeight = minMaxY.Max + tilesetOptions.TransHeight;
            tilesetOptions.OffsetX = offsetX;
            tilesetOptions.OffsetY = offsetY;

            return tilesetOptions;
        }
        /// <summary>
        /// convert to b3dm file
        /// </summary>
        /// <param name="objFile">obj file path</param>
        /// <param name="outputFile">b3dm file path</param>
        /// <param name="options">converting options</param>
        /// <returns></returns>
        public static TilesetCreationOptions WriteB3dm(String objFile,
            String outputFile, Options options = null)
        {
            var gltfOptions = new GltfOptions { Binary = true, WithBatchTable = true };
            var matParser = new MtlParser();
            var objParser = new ObjParser();
            var converter = new Converter(objParser, matParser);
            //objFile, gltfOptions,
            return WriteB3dm(converter, outputFile, options);
        }
    }
}
