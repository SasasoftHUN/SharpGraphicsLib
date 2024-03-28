using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLCore
{
    internal class GLCoreGraphicsShaderProgram : GLCoreShaderProgram, IGLGraphicsShaderProgram
    {

        #region Fields

        private readonly GraphicsShaderStages _stage;

        #endregion

        #region Properties

        public GraphicsShaderStages Stage => _stage;

        #endregion

        #region Constructors

        internal GLCoreGraphicsShaderProgram(GLCoreGraphicsDevice device, ShaderSourceText shaderSource, GraphicsShaderStages stage) : base(device, shaderSource, stage.ToShaderType())
            => _stage = stage;

        #endregion

    }
}
