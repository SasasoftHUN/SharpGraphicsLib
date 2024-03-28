using SharpGraphics.Shaders.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGraphics.Shaders.StandAloneShaderGenerator.OutputWriters
{
    internal class StandardOutputWriter : IOutputWriter
    {

        #region Fields

        private readonly bool _base64Binaries = false;
        private readonly string _shaderNamePrefix = "#**";
        private readonly string _shaderBackendPrefix = "#*";

        #endregion

        #region Constructors

        public StandardOutputWriter(bool base64Binaries = false) => _base64Binaries = base64Binaries;
        public StandardOutputWriter(bool base64Binaries, string shaderNamePrefix, string shaderBackendPrefix) : this(base64Binaries)
        {
            _shaderNamePrefix = shaderNamePrefix;
            _shaderBackendPrefix = shaderBackendPrefix;
        }

        #endregion

        #region Public Methods

        public void OutShader(GeneratedShader shader)
        {
            Console.Write(_shaderNamePrefix);
            if (!string.IsNullOrWhiteSpace(shader.Namespace))
            {
                Console.Write(shader.Namespace!);
                Console.Write('.');
            }
            Console.WriteLine(shader.Name);

            foreach (IGeneratedShaderSource shaderSource in shader.Sources)
            {
                Console.Write(_shaderBackendPrefix);
                Console.WriteLine(shaderSource.BackendName);
                switch (shaderSource)
                {
                    case IGeneratedShaderSource<string> textShaderSource: Console.WriteLine(textShaderSource.Source); break;

                    case IGeneratedShaderSource<ReadOnlyMemory<byte>> binaryShaderSource:
                        if (_base64Binaries)
                            Console.WriteLine(Convert.ToBase64String(binaryShaderSource.Source.Span));
                        else
                        {
                            ReadOnlySpan<byte> bytes = binaryShaderSource.Source.Span;
                            if (bytes.Length > 0)
                            {
                                Console.Write(bytes[0]);
                                for (int i = 1; i < bytes.Length; i++)
                                {
                                    Console.Write(',');
                                    Console.Write(bytes[i]);
                                }
                                Console.WriteLine();
                            }
                        }
                        break;
                }
            }
        }
     
        #endregion

    }
}
