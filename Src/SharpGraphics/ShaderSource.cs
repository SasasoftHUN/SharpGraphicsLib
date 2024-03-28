using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using SharpGraphics.Shaders;
using SharpGraphics.Utils;

namespace SharpGraphics
{

    public enum ShaderSourceType
    {
        Text, Binary,
    }

    public enum GraphicsShaderStages
    {
        Vertex,
        //Geometry,
        //TessellationControl,
        //TessellationEvaluation,
        Fragment,
        //Mesh
        //Raytracing
    }

    public interface IShaderSource
    {
        ShaderSourceType SourceType { get; }
    }
    public class ShaderSource<T> : IShaderSource
    {

        public ShaderSourceType SourceType { get; }
        public T Source { get; }

        protected internal ShaderSource(ShaderSourceType sourceType, T source)
        {
            SourceType = sourceType;
            Source = source;
        }

    }

    public sealed class ShaderSourceText : ShaderSource<string>
    {
        public ShaderSourceText(string source) : base(ShaderSourceType.Text, source) { }
    }

    public sealed class ShaderSourceBinary : ShaderSource<ReadOnlyMemory<byte>>
    {
        public ShaderSourceBinary(ReadOnlyMemory<byte> source) : base(ShaderSourceType.Binary, source) { }
    }

    public readonly struct GraphicsShaderSource
    {

        public readonly IShaderSource shaderSource;
        public readonly GraphicsShaderStages stage;

        public GraphicsShaderSource(IShaderSource shaderSource, GraphicsShaderStages stage)
        {
            this.shaderSource = shaderSource;
            this.stage = stage;
        }

    }


    public readonly struct CustomShaderSources
    {

        public readonly IReadOnlyDictionary<string, string>? shaderSourceTexts;
        public readonly IReadOnlyDictionary<string, ReadOnlyMemory<byte>>? shaderSourceBytes;

        public CustomShaderSources(IReadOnlyDictionary<string, string> shaderSourceTexts)
        {
            this.shaderSourceTexts = shaderSourceTexts.ToDictionary(k => k.Key, v => v.Value); //Copy for safety
            this.shaderSourceBytes = null;
        }
        public CustomShaderSources(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> shaderSourceBytes)
        {
            this.shaderSourceTexts = null;
            this.shaderSourceBytes = shaderSourceBytes.ToDictionary(k => k.Key, v => v.Value); //Copy for safety
        }
        public CustomShaderSources(IReadOnlyDictionary<string, string> shaderSourceTexts, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> shaderSourceBytes)
        {
            this.shaderSourceTexts = shaderSourceTexts.ToDictionary(k => k.Key, v => v.Value); //Copy for safety
            this.shaderSourceBytes = shaderSourceBytes.ToDictionary(k => k.Key, v => v.Value); //Copy for safety
        }

    }
    public readonly struct EmbeddedShaderSources
    {
        
        private readonly IReadOnlyDictionary<string, string> _shaderPaths;
        private readonly Assembly _assemblyOfResources;

        public EmbeddedShaderSources(IReadOnlyDictionary<string, string> shaderPaths)
        {
            _shaderPaths = shaderPaths.ToDictionary(k => k.Key, v => v.Value); //Copy for safety
            _assemblyOfResources = Assembly.GetCallingAssembly();
        }
        public EmbeddedShaderSources(IReadOnlyDictionary<string, string> shaderPaths, Assembly assemblyOfResources)
        {
            _shaderPaths = shaderPaths.ToDictionary(k => k.Key, v => v.Value); //Copy for safety
            _assemblyOfResources = assemblyOfResources;
        }

        public bool TryLoadShaderSourceText(string apiVersion, [NotNullWhen(returnValue: true)] out string? source)
        {
            if (_shaderPaths.TryGetValue(apiVersion, out string? shaderPath))
                return ResourceLoader.TryGetEmbeddedResourceString(_assemblyOfResources, shaderPath, out source);
            else
            {
                source = null;
                return false;
            }
        }
        public bool TryLoadShaderSourceBytes(string apiVersion, [NotNullWhen(returnValue: true)] out ReadOnlyMemory<byte> source)
        {
            if (_shaderPaths.TryGetValue(apiVersion, out string? shaderPath) &&
                ResourceLoader.TryGetEmbeddedResourceBytes(_assemblyOfResources, shaderPath, out byte[]? bytes))
            {
                source = bytes;
                return true;
            }
            else
            {
                source = null;
                return false;
            }
        }

    }

}
