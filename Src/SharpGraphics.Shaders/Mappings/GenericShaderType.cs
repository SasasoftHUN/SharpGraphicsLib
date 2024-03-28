using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace SharpGraphics.Shaders.Mappings
{
    public abstract class GenericShaderType
    {
        public abstract bool TryCreateType(ShaderType genericTypeParameter, [NotNullWhen(returnValue: true)] out ShaderType? resultType);
        public IEnumerable<ShaderType> TryCreateTypes(IEnumerable<ShaderType> genericTypeParameters)
        {
            List<ShaderType> resultTypes = new List<ShaderType>(genericTypeParameters.Count());

            foreach (ShaderType type in genericTypeParameters)
                if (TryCreateType(type, out ShaderType? shaderType))
                    resultTypes.Add(shaderType);

            return resultTypes;
        }
    }

    public class GenericVectorShaderType : GenericShaderType
    {
        public uint ComponentCount { get; }

        public GenericVectorShaderType(uint componentCount) => ComponentCount = componentCount;

        public override bool TryCreateType(ShaderType genericTypeParameter, [NotNullWhen(returnValue: true)] out ShaderType? resultType)
        {
            if (genericTypeParameter is PrimitiveShaderType primitiveShaderType)
            {
                resultType = ComponentCount switch
                {
                    2 => new Vector2ShaderType(primitiveShaderType),
                    3 => new Vector3ShaderType(primitiveShaderType),
                    4 => new Vector4ShaderType(primitiveShaderType),
                    _ => null
                };
                return resultType != null;
            }
            else
            {
                resultType = null;
                return false;
            }
        }
    }

    public class GenericMatrixShaderType : GenericShaderType
    {
        public uint ColumnCount { get; }

        public GenericMatrixShaderType(uint columnCount) => ColumnCount = columnCount;

        public override bool TryCreateType(ShaderType genericTypeParameter, [NotNullWhen(returnValue: true)] out ShaderType? resultType)
        {
            if (genericTypeParameter is VectorShaderType vectorShaderType)
            {
                resultType = new MatrixShaderType(vectorShaderType, ColumnCount);
                return true;
            }
            else
            {
                resultType = null;
                return false;
            }
        }
    }

    public class GenericTextureSamplerShaderType : GenericShaderType
    {
        public TextureSamplerDimensions Dimensions { get; }

        public GenericTextureSamplerShaderType(TextureSamplerDimensions dimensions) => Dimensions = dimensions;

        public override bool TryCreateType(ShaderType genericTypeParameter, [NotNullWhen(returnValue: true)] out ShaderType? resultType)
        {
            resultType = new TextureSamplerType(genericTypeParameter, Dimensions); //TODO: Test if parameter is suppported for sampling?
            return true;
        }
    }

}
