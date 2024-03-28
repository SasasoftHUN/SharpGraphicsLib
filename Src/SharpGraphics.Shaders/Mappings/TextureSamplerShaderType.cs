using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Mappings
{
    public enum TextureSamplerDimensions
    {
        Dimensions1, Dimensions2, Dimensions3,
        Cube,
        RenderPassInput,
    }
    public class TextureSamplerType : ShaderType
    {
        public override string TypeName { get; }
        public ShaderType Type { get; }
        public TextureSamplerDimensions Dimensions { get; }

        internal TextureSamplerType(ShaderType type, TextureSamplerDimensions dimensions)
        {
            string textureDimensionName = dimensions switch
            {
                TextureSamplerDimensions.Dimensions1 => "1D",
                TextureSamplerDimensions.Dimensions2 => "2D",
                TextureSamplerDimensions.Dimensions3 => "3D",
                TextureSamplerDimensions.Cube => "Cube",
                TextureSamplerDimensions.RenderPassInput => "RenderPassInput",
                _ => "",
            };
            TypeName = $"Texture{textureDimensionName}{type}";

            Type = type;
            Dimensions = dimensions;
        }
    }
}
