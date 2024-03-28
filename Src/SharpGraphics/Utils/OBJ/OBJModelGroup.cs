using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.Utils.OBJ
{
    public class OBJModelGroup
    {

        [StructLayout(LayoutKind.Sequential)]
        public struct Vertex
        {
            public Vector3 position;
            public Vector3 normal;
            public Vector2 textureuv;
        }

        public Vertex[] Vertices { get; }
        public uint[] Indices { get; }
        public bool IndicesCanBeShort { get; }
        public int MaterialIndex { get; }

        internal OBJModelGroup(Vertex[] vertices, uint[] indices, bool indicesCanBeShort, int materialIndex)
        {
            Vertices = vertices;
            Indices = indices;
            IndicesCanBeShort = indicesCanBeShort;
            MaterialIndex = materialIndex;
        }

    }
}
