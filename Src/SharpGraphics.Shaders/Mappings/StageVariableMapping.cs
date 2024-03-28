using System;
using System.Collections.Generic;
using System.Text;
using SharpGraphics.Shaders.Generator;

namespace SharpGraphics.Shaders.Mappings
{

    public enum StageVariableType { In, Out }

    internal abstract class StageVariableMapping

    {

        public string Name { get; }
        public StageVariableType Type { get; }
        public ShaderStage Stage { get; }
        public ShaderType ShaderType { get; }

        protected StageVariableMapping(string name, StageVariableType type, ShaderStage stage, ShaderType shaderType)
        {
            Name = name;
            Type = type;
            Stage = stage;
            ShaderType = shaderType;
        }

    }

    internal sealed class StageInVariableMapping : StageVariableMapping
    {
        public StageInVariableMapping(string name, ShaderStage stage, ShaderType shaderType) : base(name, StageVariableType.In, stage, shaderType) { }
    }

    internal sealed class StageOutVariableMapping : StageVariableMapping
    {
        public StageOutVariableMapping(string name, ShaderStage stage, ShaderType shaderType) : base(name, StageVariableType.Out, stage, shaderType) { }
    }
}
