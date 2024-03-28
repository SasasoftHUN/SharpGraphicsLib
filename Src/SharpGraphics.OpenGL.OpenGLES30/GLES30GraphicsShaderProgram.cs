using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLES30
{
    internal class GLES30GraphicsShaderProgram : GLES30ShaderProgram, IGLGraphicsShaderProgram
    {

        #region Fields

        private readonly GraphicsShaderStages _stage;

        #endregion

        #region Properties

        public GraphicsShaderStages Stage => _stage;

        #endregion

        #region Constructors

        internal GLES30GraphicsShaderProgram(GLES30GraphicsDevice device, ShaderSourceText shaderSource, GraphicsShaderStages stage) : base(device, shaderSource, stage.ToShaderType())
            => _stage = stage;

        #endregion

    }
}
