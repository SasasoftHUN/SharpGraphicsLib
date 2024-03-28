using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL
{
    public interface IGLShaderProgram : IShaderProgram, IGLResource
    {

        int ID { get; }

    }

    public interface IGLGraphicsShaderProgram : IGraphicsShaderProgram, IGLShaderProgram
    {

    }
}
