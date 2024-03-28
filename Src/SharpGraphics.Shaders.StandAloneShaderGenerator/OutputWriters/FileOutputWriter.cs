using SharpGraphics.Loggers;
using SharpGraphics.Shaders.Generator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGraphics.Shaders.StandAloneShaderGenerator.OutputWriters
{
    internal class FileOutputWriter : IOutputWriter
    {

        #region Fields

        #endregion

        #region Constructors

        #endregion

        #region Public Methods

        public void OutShader(GeneratedShader shader)
        {
            foreach (IGeneratedShaderSource shaderSource in shader.Sources)
            {
                string fileExtension = shaderSource.BackendName switch
                {
                    ['S','P','I','R',..] => ".spv",

                    ['G','L','S','L',..] => shader.Stage switch
                    {
                        ShaderStage.Vertex => ".vert",
                        ShaderStage.Fragment => ".frag",
                        _ => ".glsl",
                    },

                    ['H','L','S','L',..] => ".hlsl",

                    _ => shaderSource.SourceType switch
                    {
                        ShaderSourceType.Text => ".txt",
                        ShaderSourceType.Binary => ".bin",
                        _ => ".txt",
                    }
                };

                string fileName = string.IsNullOrWhiteSpace(shader.Namespace) ?
                    $"{shader.Namespace!.Replace('.', '-')}-{shader.Name}.{shaderSource.BackendName}{fileExtension}" :
                    $"{shader.Name}.{shaderSource.BackendName}{fileExtension}";

                using FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

                switch (shaderSource)
                {
                    case IGeneratedShaderSource<string> textShaderSource:
                        {
                            using StreamWriter sw = new StreamWriter(fs);
                            sw.Write(textShaderSource.Source);
                        }
                        break;

                    case IGeneratedShaderSource<ReadOnlyMemory<byte>> binaryShaderSource:
                        fs.Write(binaryShaderSource.Source.Span);
                        break;
                }
            }
        }

        #endregion

    }
}
