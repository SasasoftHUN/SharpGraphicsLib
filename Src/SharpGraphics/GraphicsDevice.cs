using SharpGraphics.GraphicsViews;
using SharpGraphics.Shaders;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using static SharpGraphics.GraphicsSwapChain;

namespace SharpGraphics
{
    public abstract class GraphicsDevice : IDevice
    {

        #region Fields

        private bool _isDisposed;

        protected readonly GraphicsManagement _management;

        #endregion

        #region Properties

        public bool IsDisposed => _isDisposed;

        public abstract GraphicsDeviceInfo DeviceInfo { get; }
        public abstract GraphicsDeviceLimits Limits { get; }
        public abstract GraphicsDeviceFeatures Features { get; }

        public abstract ReadOnlySpan<GraphicsCommandProcessor> CommandProcessors { get; }

        #endregion

        #region Constructors

        protected GraphicsDevice(GraphicsManagement management)
            => _management = management;

        // Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~GraphicsDevice()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        #endregion

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null
                //SwapChain.Dispose(); //Need to call in derived before Device is disposed...

                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public abstract void WaitForIdle();

#if NETUNIFIED
        public virtual IShaderSource CreateShaderSource<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>() where T : ShaderBase, new()
#else
        public virtual IShaderSource CreateShaderSource<T>() where T : ShaderBase, new()
#endif
        {
            T shader = new T();
            foreach (ShaderAPIVersion shaderAPIVersion in Features.ShaderAPIVersions)
            {
                switch (shaderAPIVersion.type)
                {
                    case ShaderSourceType.Text:
                        if (shader.TryGetSourceText(shaderAPIVersion.name, out string? text))
                            return new ShaderSourceText(text);
                        break;
                    case ShaderSourceType.Binary:
                        if (shader.TryGetSourceBinary(shaderAPIVersion.name, out ReadOnlyMemory<byte> bytes))
                            return new ShaderSourceBinary(bytes);
                        break;
                }
            }
            throw new Exception($"{Features.ShaderAPIVersions.Select(v => v.name).Aggregate((v1, v2) => $"{v1}, {v2}")} Shader Version(s) not found for Shader Class!");
        }
        public virtual IShaderSource CreateShaderSource(in CustomShaderSources shaderSources)
        {
            foreach (ShaderAPIVersion shaderAPIVersion in Features.ShaderAPIVersions)
            {
                switch (shaderAPIVersion.type)
                {
                    case ShaderSourceType.Text:
                        if (shaderSources.shaderSourceTexts != null && shaderSources.shaderSourceTexts.TryGetValue(shaderAPIVersion.name, out string? text))
                            return new ShaderSourceText(text);
                        break;
                    case ShaderSourceType.Binary:
                        if (shaderSources.shaderSourceBytes != null && shaderSources.shaderSourceBytes.TryGetValue(shaderAPIVersion.name, out ReadOnlyMemory<byte> bytes))
                            return new ShaderSourceBinary(bytes);
                        break;
                }
            }
            throw new Exception($"{Features.ShaderAPIVersions.Select(v => v.name).Aggregate((v1, v2) => $"{v1}, {v2}")} Shader Version(s) not found for Shader Class!");
        }
        public virtual IShaderSource CreateShaderSource(in EmbeddedShaderSources shaderSources)
        {
            foreach (ShaderAPIVersion shaderAPIVersion in Features.ShaderAPIVersions)
            {
                switch (shaderAPIVersion.type)
                {
                    case ShaderSourceType.Text:
                        if (shaderSources.TryLoadShaderSourceText(shaderAPIVersion.name, out string? text))
                            return new ShaderSourceText(text);
                        break;
                    case ShaderSourceType.Binary:
                        if (shaderSources.TryLoadShaderSourceBytes(shaderAPIVersion.name, out ReadOnlyMemory<byte> bytes))
                            return new ShaderSourceBinary(bytes);
                        break;
                }
            }
            throw new Exception($"{Features.ShaderAPIVersions.Select(v => v.name).Aggregate((v1, v2) => $"{v1}, {v2}")} Shader Version(s) not found for Shader Class!");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDataBuffer CreateDataBuffer(DataBufferType bufferType, ulong size, in MemoryType memoryType)
            => memoryType.IsDeviceOnly ? CreateDeviceOnlyDataBuffer(bufferType, size) : CreateMappableDataBuffer(bufferType, memoryType.MappableType, size);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDataBuffer<T> CreateDataBuffer<T>(DataBufferType bufferType, int dataCapacity, in MemoryType memoryType, bool isAligned = true) where T : unmanaged
            => memoryType.IsDeviceOnly ? CreateDeviceOnlyDataBuffer<T>(bufferType, (uint)dataCapacity) : CreateMappableDataBuffer<T>(bufferType, memoryType.MappableType, (uint)dataCapacity);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDataBuffer<T> CreateDataBuffer<T>(DataBufferType bufferType, uint dataCapacity, in MemoryType memoryType, bool isAligned = true) where T : unmanaged
            => memoryType.IsDeviceOnly ? CreateDeviceOnlyDataBuffer<T>(bufferType, dataCapacity) : CreateMappableDataBuffer<T>(bufferType, memoryType.MappableType, dataCapacity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDeviceOnlyDataBuffer CreateDeviceOnlyDataBuffer(DataBufferType bufferType, ulong size) => CreateDeviceOnlyDataBuffer<byte>(bufferType, (uint)size, false);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDeviceOnlyDataBuffer<T> CreateDeviceOnlyDataBuffer<T>(DataBufferType bufferType, int dataCapacity, bool isAligned = true) where T : unmanaged
            => CreateDeviceOnlyDataBuffer<T>(bufferType, (uint)dataCapacity, isAligned);
        public abstract IDeviceOnlyDataBuffer<T> CreateDeviceOnlyDataBuffer<T>(DataBufferType bufferType, uint dataCapacity = 0u, bool isAligned = true) where T : unmanaged;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IMappableDataBuffer CreateMappableDataBuffer(DataBufferType bufferType, MappableMemoryType memoryType, ulong size) => CreateMappableDataBuffer<byte>(bufferType, memoryType, (uint)size, false);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IMappableDataBuffer<T> CreateMappableDataBuffer<T>(DataBufferType bufferType, MappableMemoryType memoryType, int dataCapacity, bool isAligned = true) where T : unmanaged
            => CreateMappableDataBuffer<T>(bufferType, memoryType, (uint)dataCapacity, isAligned);
        public abstract IMappableDataBuffer<T> CreateMappableDataBuffer<T>(DataBufferType bufferType, MappableMemoryType memoryType, uint dataCapacity = 0u, bool isAligned = true) where T : unmanaged;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IStagingDataBuffer CreateStagingDataBuffer(ulong size) => CreateStagingDataBuffer<byte>(DataBufferType.Unknown, (uint)size);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IStagingDataBuffer<T> CreateStagingDataBuffer<T>(DataBufferType alignmentType, int dataCapacity) where T : unmanaged
            => CreateStagingDataBuffer<T>(alignmentType, (uint)dataCapacity);
        public abstract IStagingDataBuffer<T> CreateStagingDataBuffer<T>(DataBufferType alignmentType, uint dataCapacity = 0u) where T : unmanaged;


        public abstract PipelineResourceLayout CreatePipelineResourceLayout(in PipelineResourceProperties properties);

        public abstract ITexture2D CreateTexture2D(DataFormat format, Vector2UInt resolution, TextureType textureType, in MemoryType memoryType, uint mipLevels = 0u);
        public abstract ITextureCube CreateTextureCube(DataFormat format, Vector2UInt resolution, TextureType textureType, in MemoryType memoryType, uint mipLevels = 0u);
        public abstract TextureSampler CreateTextureSampler(in TextureSamplerConstruction construction);

        #endregion

    }

    public class GraphicsDeviceCreationException : Exception
    {

        public GraphicsDeviceCreationException() { }
        public GraphicsDeviceCreationException(string message) : base(message) { }

    }

}
