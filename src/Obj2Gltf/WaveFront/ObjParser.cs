﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Arctron.Obj2Gltf.Geom;

namespace Arctron.Obj2Gltf.WaveFront
{
    /// <summary>
    /// parse an obj file 
    /// </summary>
    public class ObjParser
    {
        private static class Statements
        {
            public const String Comment = "#";
            public const String MtlLib = "mtllib";
            public const String Vertex = "v";
            public const String VectorNormal = "vn";
            public const String VectorTexture = "vt";
            public const String Group = "g";
            public const String UseMaterial = "usemtl";
            public const String Face = "f";
        }


        private static FaceVertex GetVertex(String vStr)
        {
            var v1Str = vStr.Split('/');
            if (v1Str.Length >= 3)
            {
                var v = Int32.Parse(v1Str[0]);
                var t = 0;
                if (!String.IsNullOrEmpty(v1Str[1]))
                {
                    t = Int32.Parse(v1Str[1]);
                }
                var n = Int32.Parse(v1Str[2]);
                return new FaceVertex(v, t, n);
            }
            else if (v1Str.Length >= 2)
            {
                return new FaceVertex(Int32.Parse(v1Str[0]), Int32.Parse(v1Str[1]), 0);
            }
            return new FaceVertex(Int32.Parse(v1Str[0]));
        }

        private static String[] SplitLine(String line)
        {
            return line.Split(new Char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static Boolean StartWith(String line, String str)
        {
            return line.StartsWith(str) && (line[str.Length] == ' ' || line[str.Length] == '\t');
        }
        /// <summary>
        /// get parsed obj model
        /// </summary>
        /// <returns></returns>
        public ObjModel Parse(String objFilePath, Encoding encoding = null)
        {
            if (String.IsNullOrEmpty(objFilePath)) throw new ArgumentNullException(nameof(objFilePath));

            var model = new ObjModel
            {
                Name = Path.GetFileNameWithoutExtension(objFilePath)
            };
            var _reader = encoding != null
               ? new StreamReader(objFilePath, encoding)
               : new StreamReader(objFilePath);

            using (_reader)
            {
                var currentMaterialName = "default";
                var currentGeometries = model.GetOrAddGeometries("default");
                Face currentFace = null;

                while (!_reader.EndOfStream)
                {
                    var line = _reader.ReadLine().Trim();
                    if (String.IsNullOrEmpty(line)) continue;
                    if (line.StartsWith(Statements.Comment)) continue;
                    if (StartWith(line, Statements.MtlLib))
                    {
                        model.MatFilename = line.Substring(6).Trim();
                    }
                    else if (StartWith(line, Statements.Vertex))
                    {
                        var vStr = line.Substring(2).Trim();
                        var strs = SplitLine(vStr);
                        var v = new Vec3(Double.Parse(strs[0]), Double.Parse(strs[1]), Double.Parse(strs[2]));
                        model.Vertices.Add(v);
                    }
                    else if (StartWith(line, Statements.VectorNormal))
                    {
                        var vnStr = line.Substring(3).Trim();
                        var strs = SplitLine(vnStr);
                        var vn = new Vec3(Double.Parse(strs[0]), Double.Parse(strs[1]), Double.Parse(strs[2]));
                        model.Normals.Add(vn);
                    }
                    else if (StartWith(line, Statements.VectorTexture))
                    {
                        var vtStr = line.Substring(3).Trim();
                        var strs = SplitLine(vtStr);
                        var vt = new Vec2(Double.Parse(strs[0]), Double.Parse(strs[1]));
                        model.Uvs.Add(vt);
                    }
                    else if (StartWith(line, Statements.Group))
                    {
                        var gStr = line.Substring(2);
                        var groupNames = SplitLine(gStr);

                        currentGeometries = model.GetOrAddGeometries(groupNames);
                    }
                    else if (StartWith(line, Statements.UseMaterial))
                    {
                        var umtl = line.Substring(7).Trim();
                        currentMaterialName = umtl;
                    }
                    else if (StartWith(line, Statements.Face))
                    {
                        var fStr = line.Substring(2).Trim();
                        currentFace = new Face(currentMaterialName);
                        var strs = SplitLine(fStr);
                        if (strs.Length < 3) continue; // ignore face that has less than 3 vertices
                        if (strs.Length == 3)
                        {
                            var v1 = GetVertex(strs[0]);
                            var v2 = GetVertex(strs[1]);
                            var v3 = GetVertex(strs[2]);
                            var f = new FaceTriangle(v1, v2, v3);
                            currentFace.Triangles.Add(f);
                        }
                        else if (strs.Length == 4)
                        {
                            var v1 = GetVertex(strs[0]);
                            var v2 = GetVertex(strs[1]);
                            var v3 = GetVertex(strs[2]);
                            var f = new FaceTriangle(v1, v2, v3);
                            currentFace.Triangles.Add(f);
                            var v4 = GetVertex(strs[3]);
                            var ff = new FaceTriangle(v1, v3, v4);
                            currentFace.Triangles.Add(ff);
                        }
                        else //if (strs.Length > 4)
                        {
                            var points = new List<Vec3>();
                            for (var i = 0; i < strs.Length; i++)
                            {
                                var vv = GetVertex(strs[i]);
                                var p = model.Vertices[vv.V - 1];
                                points.Add(p);
                            }
                            var planeAxis = GeomUtil.ComputeProjectTo2DArguments(points);
                            if (planeAxis != null)
                            {
                                var points2D = GeomUtil.CreateProjectPointsTo2DFunction(planeAxis, points);
                                var indices = PolygonPipeline.Triangulate(points2D, null);
                                if (indices.Length == 0)
                                {
                                    // TODO:
                                }
                                for (var i = 0; i < indices.Length - 2; i += 3)
                                {
                                    var vv1 = GetVertex(strs[indices[i]]);
                                    var vv2 = GetVertex(strs[indices[i + 1]]);
                                    var vv3 = GetVertex(strs[indices[i + 2]]);
                                    var ff = new FaceTriangle(vv1, vv2, vv3);
                                    currentFace.Triangles.Add(ff);
                                }
                            }
                            else
                            {
                                // TODO:
                            }
                        }
                        foreach (var currentGeometry in currentGeometries)
                        {
                            currentGeometry.Faces.Add(currentFace);
                        }
                    }
                    else
                    {
                        //var strs = SplitLine(line);
                    }
                }
            }
            return model;
        }
    }
}
