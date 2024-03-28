using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics
{
    public interface IShaderProgram : IDisposable
    {

        

    }

    //TODO: Implement for Compute Shaders: public interface IComputeShaderProgram : IShaderProgram


    public interface IGraphicsShaderProgram : IShaderProgram
    {

        GraphicsShaderStages Stage { get; }

    }


    public readonly struct GraphicsShaderPrograms : IDisposable
    {

        private readonly IGraphicsShaderProgram[] _shaderPrograms;

        public ReadOnlySpan<IGraphicsShaderProgram> ShaderPrograms => _shaderPrograms;

        internal GraphicsShaderPrograms(IGraphicsShaderProgram[] shaderPrograms)
            => _shaderPrograms = shaderPrograms; //No copy for safety, only used internally

        public void Dispose()
        {
            if (_shaderPrograms != null)
                foreach (IGraphicsShaderProgram shaderProgram in _shaderPrograms)
                    if (shaderProgram != null)
                        shaderProgram.Dispose();
        }

        public static implicit operator ReadOnlySpan<IGraphicsShaderProgram>(in GraphicsShaderPrograms shaderPrograms) => shaderPrograms.ShaderPrograms;

    }

}
