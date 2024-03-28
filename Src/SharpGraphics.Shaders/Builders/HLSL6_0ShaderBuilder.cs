using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGraphics.Shaders.Generator;
using SharpGraphics.Shaders.Mappings;
using SharpGraphics.Shaders.Validators.DirectXShaderCompiler;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Builders
{
    internal class HLSL6_0ShaderBuilder : HLSLShaderBuilderBase
    {

        #region Fields

        protected int _inLocation = 0;
        protected int _outLocation = 0;

        #endregion

        #region Properties

        protected override string TargetShaderModel => "6_0";

        public override string BackendName => "HLSL6_0";

        #endregion

        #region Constructors

        public HLSL6_0ShaderBuilder(ShaderLanguageMappings mappings) : base(mappings) { }
        public HLSL6_0ShaderBuilder(ShaderLanguageMappings mappings, IDirectXShaderCompiler dxc) : base(mappings, dxc) { }

        #endregion

        #region Protected Methods



        #endregion

    }
}
