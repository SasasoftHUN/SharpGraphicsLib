using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Mappings
{
    public class MatrixShaderType : ShaderType
    {
        public override string TypeName { get; }
        public VectorShaderType ColumnType { get; }
        public uint RowCount { get; }

        internal MatrixShaderType(VectorShaderType columnType, uint rowCount)
        {
            TypeName = $"Matrix{rowCount}x{columnType.ComponentCount}{columnType.ComponentType}";

            ColumnType = columnType;
            RowCount = rowCount;
        }
    }
}
