using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using static SharpGraphics.Utils.OBJ.OBJModelGroup;

namespace SharpGraphics.Utils.OBJ
{
    public static class OBJImporter
    {

        #region Private Methods

        private static Vector2 ParseVertex2(string[] lineSplit)
        {
            Vector2 position = Vector2.Zero;
            float.TryParse(lineSplit[1], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out position.X);
            float.TryParse(lineSplit[2], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out position.Y);
            return position;
        }
        private static Vector3 ParseVertex3(string[] lineSplit, float scale = 1f)
        {
            Vector3 position = Vector3.Zero;
            float.TryParse(lineSplit[1], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out position.X);
            float.TryParse(lineSplit[2], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out position.Y);
            float.TryParse(lineSplit[3], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out position.Z);
            return position * scale;
        }
        private static string AggregateFrom(string[] lineSplit, int skip)
            => lineSplit.Skip(skip).Aggregate(static (a, b) => a + " " + b);


        private static IReadOnlyList<OBJMaterial> ImportMaterial(StreamReader mtlReader)
        {
            List<OBJMaterial> materials = new List<OBJMaterial>();

            OBJMaterial? material = null;

            char[] separators = new char[] { ' ' , '\t' };
            string? line;
            while (null != (line = mtlReader.ReadLine()))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] lineSplit = line.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                switch (lineSplit[0])
                {
                    case "newmtl":
                        if (material != null)
                            materials.Add(material);
                        material = new OBJMaterial()
                        {
                            Name = lineSplit.Length > 0 ? lineSplit[1] : "",
                        };
                        break;
                }

                if (material != null)
                    switch (lineSplit[0])
                    {
                        case "Ka": material.Ka = ParseVertex3(lineSplit); break;
                        case "Kd": material.Kd = ParseVertex3(lineSplit); break;
                        case "Ks": material.Ks = ParseVertex3(lineSplit); break;
                        case "Ns":
                            if (float.TryParse(lineSplit[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float ns))
                                material.Ns = ns;
                            break;

                        case "map_Ka": material.MapKa = AggregateFrom(lineSplit, 1); break;
                        case "map_Kd": material.MapKd = AggregateFrom(lineSplit, 1); break;
                        case "map_Ks": material.MapKs = AggregateFrom(lineSplit, 1); break;
                        case "map_Disp": material.MapDisp = AggregateFrom(lineSplit, 1); break;
                        case "map_d": material.MapD = AggregateFrom(lineSplit, 1); break;
                    }
            }

            if (material != null)
                materials.Add(material);

            return materials;
        }
        private static OBJMaterial[] ImportMaterials(StreamReader[] mtlReaders)
        {
            if (mtlReaders == null)
                return new OBJMaterial[] { new OBJMaterial() };
            else
            {
                List<OBJMaterial> materials = new List<OBJMaterial>() { new OBJMaterial() };
                foreach (StreamReader mtlReader in mtlReaders)
                    if (mtlReader != null)
                        materials.AddRange(ImportMaterial(mtlReader));
                return materials.ToArray();
            }
        }


        private static uint ParseVertexIndices(List<Vector3> positions, List<Vector3> normals, List<Vector2> textureUVs, Dictionary<string, uint> parsedVertices, List<Vertex> vertices, string vertexLine)
        {
            string[] vertexLineSplit = vertexLine.Split('/');
            Vertex vertex = new Vertex();

            if (int.TryParse(vertexLineSplit[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int index))
                vertex.position = index > 0 ? positions[index - 1] : positions[positions.Count - index];
            if (vertexLineSplit.Length > 1 && int.TryParse(vertexLineSplit[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out index))
                vertex.textureuv = index > 0 ? textureUVs[index - 1] : textureUVs[textureUVs.Count - index];
            if (vertexLineSplit.Length > 2 && int.TryParse(vertexLineSplit[2], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out index))
                vertex.normal = index > 0 ? normals[index - 1] : normals[normals.Count - index];

            uint vIndex = (uint)vertices.Count;
            vertices.Add(vertex);
            parsedVertices[vertexLine] = vIndex;
            return vIndex;
        }
        private static uint ParseVertex(List<Vector3> positions, List<Vector3> normals, List<Vector2> textureUVs, Dictionary<string, uint> parsedVertices, List<Vertex> vertices, string vertexLine)
            => parsedVertices.TryGetValue(vertexLine, out uint index) ? index : ParseVertexIndices(positions, normals, textureUVs, parsedVertices, vertices, vertexLine);
        private static void ParseFace(List<Vector3> positions, List<Vector3> normals, List<Vector2> textureUVs, Dictionary<string, uint> parsedVertices, List<Vertex> vertices, List<uint> groupIndices, string[] lineSplit)
        {
            int max = lineSplit.Length - 2;
            for (int i = 1; i < max; i++)
            {
                groupIndices.Add(ParseVertex(positions, normals, textureUVs, parsedVertices, vertices, lineSplit[1]));
                groupIndices.Add(ParseVertex(positions, normals, textureUVs, parsedVertices, vertices, lineSplit[i + 1]));
                groupIndices.Add(ParseVertex(positions, normals, textureUVs, parsedVertices, vertices, lineSplit[i + 2]));
            }
        }
        private static int FindMaterialIndex(OBJMaterial[] materials, string name)
        {
            for (int i = 0; i < materials.Length; i++)
                if (string.Equals(materials[i].Name, name, StringComparison.Ordinal))
                    return i;
            return 0;
        }

        private static void FinalizeGroup(List<OBJModelGroup> groups, string groupName, List<Vertex> vertices, List<uint> groupIndices, int materialIndex)
        {
            if (groupIndices.Count > 0)
            {
                List<Vertex> groupVertices = new List<Vertex>(groupIndices.Count);
                List<uint> finalIndices = new List<uint>(groupIndices.Count);
                bool indicesCanBeUShort = true;
                Dictionary<uint, uint> indexMap = new Dictionary<uint, uint>(groupIndices.Count);

                foreach (uint index in groupIndices)
                {
                    if (indexMap.TryGetValue(index, out uint i))
                        finalIndices.Add(i);
                    else
                    {
                        i = (uint)groupVertices.Count;
                        indexMap[index] = i;
                        finalIndices.Add(i);
                        groupVertices.Add(vertices[(int)index]);
                        if (i > ushort.MaxValue)
                            indicesCanBeUShort = false;
                    }
                }

                groups.Add(new OBJModelGroup(groupVertices.ToArray(), finalIndices.ToArray(), indicesCanBeUShort, materialIndex == -1 ? 0 : materialIndex));
            }
        }

        #endregion

        #region Public Methods

        public static OBJModel Import(StreamReader objReader, StreamReader[] mtlReaders, float scale = 1f)
        {
            OBJMaterial[] materials = ImportMaterials(mtlReaders);
            
            List<Vector3> positions = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> textureUVs = new List<Vector2>();

            List<OBJModelGroup> groups = new List<OBJModelGroup>();
            string groupName = "";
            Dictionary<string, uint> parsedVertices = new Dictionary<string, uint>();
            List<Vertex> vertices = new List<Vertex>();
            List<uint> groupIndices = new List<uint>();
            int materialIndex = -1;

            string? line;
            char[] separators = new char[] { ' ' , '\t' };
            while (null != (line = objReader.ReadLine()))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] lineSplit = line.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                switch (lineSplit[0])
                {
                    case "v": positions.Add(ParseVertex3(lineSplit, scale)); break;
                    case "vt": textureUVs.Add(ParseVertex2(lineSplit)); break;
                    case "vn": normals.Add(ParseVertex3(lineSplit)); break;

                    case "f": ParseFace(positions, normals, textureUVs, parsedVertices, vertices, groupIndices, lineSplit); break;

                    case "g":
                        FinalizeGroup(groups, groupName, vertices, groupIndices, materialIndex);
                        groupIndices.Clear();
                        groupName = lineSplit.Length > 0 ? lineSplit[1] : "";
                        materialIndex = -1;
                        break;

                    case "usemtl":
                        if (materialIndex == -1)
                            materialIndex = FindMaterialIndex(materials, lineSplit[1]);
                        else
                        {
                            FinalizeGroup(groups, groupName, vertices, groupIndices, materialIndex);
                            groupIndices.Clear();
                            materialIndex = FindMaterialIndex(materials, lineSplit[1]);
                        }
                        break;
                }
            }

            FinalizeGroup(groups, groupName, vertices, groupIndices, materialIndex);

            return new OBJModel(groups.ToArray(), materials);
        }

        #endregion

    }
}
