using SharpGraphics.Shaders.Mappings;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Generator
{

    public enum ShaderStage
    {
        //Compute,
        Vertex,
        //Geometry,
        //TessellationControl,
        //TessellationEvaluation,
        Fragment,
        //Mesh
        //Raytracing
    }

    public interface IGeneratedShaderSource
    {
        string BackendName { get; }
        public ShaderSourceType SourceType { get; }

        string AsToStringLiteral();
    }
    public interface IGeneratedShaderSource<T> : IGeneratedShaderSource
    {
        public T Source { get; }
    }

    public abstract class GeneratedShaderSource<T> : IGeneratedShaderSource<T>
    {
        public string BackendName { get; }
        public T Source { get; }
        public ShaderSourceType SourceType { get; }

        protected GeneratedShaderSource(string backendName, T source, ShaderSourceType sourceType)
        {
            BackendName = backendName;
            Source = source;
            SourceType = sourceType;
        }

        public abstract string AsToStringLiteral();
    }
    public sealed class GeneratedTextShaderSource : GeneratedShaderSource<string>
    {
        public GeneratedTextShaderSource(string backendName, string source) : base(backendName, source, ShaderSourceType.Text) { }
        public override string AsToStringLiteral() => $"@\"{Source}\"";
    }
    public sealed class GeneratedBinaryShaderSource : GeneratedShaderSource<ReadOnlyMemory<byte>>
    {
        private StringBuilder? _binaryStringBuilder;
        public GeneratedBinaryShaderSource(string backendName, ReadOnlyMemory<byte> source) : base(backendName, source, ShaderSourceType.Binary) { }
        public override string AsToStringLiteral()
        {
            if (_binaryStringBuilder == null)
            {
                _binaryStringBuilder = new StringBuilder();
                _binaryStringBuilder.Append("new byte[] { ");
                if (Source.Length > 0)
                {
                    ReadOnlySpan<byte> bytes = Source.Span;
                    _binaryStringBuilder.Append(bytes[0]);
                    for (int i = 1; i < Source.Length; i++)
                    {
                        _binaryStringBuilder.Append(", ");
                        _binaryStringBuilder.Append(bytes[i]);
                    }
                }
                _binaryStringBuilder.Append("}");
            }
            return _binaryStringBuilder.ToString();
        }
    }

    public class GeneratedShader
    {

        private readonly List<IGeneratedShaderSource> _sources = new List<IGeneratedShaderSource>();

        public string? Namespace { get; }
        public string Name { get; }
        public IReadOnlyList<IGeneratedShaderSource> Sources => _sources;
        public ShaderStage Stage { get; }

        public GeneratedShader(string? ns, string name, ShaderStage stage)
        {
            Namespace = ns;
            Name = name;
            Stage = stage;
        }

        public void AddShaderSource(IGeneratedShaderSource shaderSource) => _sources.Add(shaderSource);

    }
}
