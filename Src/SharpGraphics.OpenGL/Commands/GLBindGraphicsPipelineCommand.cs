using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.Commands
{
    public sealed class GLBindGraphicsPipelineCommand : IGLCommand
    {
        
        private readonly GLGraphicsPipeline _pipeline;

        public GLBindGraphicsPipelineCommand(GLGraphicsPipeline pipeline) => _pipeline = pipeline;

        public void Execute() => _pipeline.GLBind();

        public override string ToString() => $"Bind Graphics Pipeline (Program ID: {_pipeline.ProgramID})";

    }
}
