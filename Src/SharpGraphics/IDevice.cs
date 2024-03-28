using SharpGraphics.Shaders;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace SharpGraphics
{
    public interface IDevice : IDisposable
    {

        bool IsDisposed { get; }

        GraphicsDeviceInfo DeviceInfo { get; }
        GraphicsDeviceLimits Limits { get; }
        GraphicsDeviceFeatures Features { get; }

        ReadOnlySpan<GraphicsCommandProcessor> CommandProcessors { get; }

        void WaitForIdle();

#if NETUNIFIED
        IShaderSource CreateShaderSource<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>() where T : ShaderBase, new();
#else
        IShaderSource CreateShaderSource<T>() where T : ShaderBase, new();
#endif
        IShaderSource CreateShaderSource(in CustomShaderSources shaderSources);
        IShaderSource CreateShaderSource(in EmbeddedShaderSources shaderSources);

        IDataBuffer CreateDataBuffer(DataBufferType bufferType, ulong size, in MemoryType memoryType);
        IDataBuffer<T> CreateDataBuffer<T>(DataBufferType bufferType, int dataCapacity, in MemoryType memoryType, bool isAligned = true) where T : unmanaged;
        IDataBuffer<T> CreateDataBuffer<T>(DataBufferType bufferType, uint dataCapacity, in MemoryType memoryType, bool isAligned = true) where T : unmanaged;
        IDeviceOnlyDataBuffer CreateDeviceOnlyDataBuffer(DataBufferType bufferType, ulong size);
        IDeviceOnlyDataBuffer<T> CreateDeviceOnlyDataBuffer<T>(DataBufferType bufferType, int dataCapacity, bool isAligned = true) where T : unmanaged;
        IDeviceOnlyDataBuffer<T> CreateDeviceOnlyDataBuffer<T>(DataBufferType bufferType, uint dataCapacity = 0u, bool isAligned = true) where T : unmanaged;
        IMappableDataBuffer CreateMappableDataBuffer(DataBufferType bufferType, MappableMemoryType memoryType, ulong size);
        IMappableDataBuffer<T> CreateMappableDataBuffer<T>(DataBufferType bufferType, MappableMemoryType memoryType, int dataCapacity, bool isAligned = true) where T : unmanaged;
        IMappableDataBuffer<T> CreateMappableDataBuffer<T>(DataBufferType bufferType, MappableMemoryType memoryType, uint dataCapacity = 0u, bool isAligned = true) where T : unmanaged;
        IStagingDataBuffer CreateStagingDataBuffer(ulong size);
        IStagingDataBuffer<T> CreateStagingDataBuffer<T>(DataBufferType alignmentType, int dataCapacity) where T : unmanaged;
        IStagingDataBuffer<T> CreateStagingDataBuffer<T>(DataBufferType alignmentType, uint dataCapacity = 0u) where T : unmanaged;

        ITexture2D CreateTexture2D(DataFormat format, Vector2UInt resolution, TextureType textureType, in MemoryType memoryType, uint mipLevels = 0u);
        ITextureCube CreateTextureCube(DataFormat format, Vector2UInt resolution, TextureType textureType, in MemoryType memoryType, uint mipLevels = 0u);
        TextureSampler CreateTextureSampler(in TextureSamplerConstruction construction);

        PipelineResourceLayout CreatePipelineResourceLayout(in PipelineResourceProperties properties);

    }
}
