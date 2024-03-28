using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Mappings
{
    public abstract class PrimitiveShaderType : ShaderType
    {
        protected string _typeName;

        public override string TypeName => _typeName;

        public PrimitiveShaderTypes Type { get; }

        protected PrimitiveShaderType(PrimitiveShaderTypes type)
        {
            _typeName = type.ToString();
            Type = type;
        }
    }

    public class VoidShaderType : PrimitiveShaderType
    {
        internal VoidShaderType() : base(PrimitiveShaderTypes.Void) { }
    }
    public class BoolShaderType : PrimitiveShaderType
    {
        internal BoolShaderType() : base(PrimitiveShaderTypes.Bool) { }
    }

    public abstract class NumericShaderType : PrimitiveShaderType
    {
        public ShaderTypePrecisions Precision { get; }

        protected NumericShaderType(PrimitiveShaderTypes type, ShaderTypePrecisions precision) : base(type)
        {
            _typeName += ((uint)precision).ToString();
            Precision = precision;
        }
    }
    public class IntShaderType : NumericShaderType
    {
        internal IntShaderType(ShaderTypePrecisions precision) : base(PrimitiveShaderTypes.Int, precision) { }
    }
    public class UIntShaderType : NumericShaderType
    {
        internal UIntShaderType(ShaderTypePrecisions precision) : base(PrimitiveShaderTypes.UInt, precision) { }
    }
    public class FloatShaderType : NumericShaderType
    {
        internal FloatShaderType(ShaderTypePrecisions precision) : base(PrimitiveShaderTypes.Float, precision) { }
    }
}
