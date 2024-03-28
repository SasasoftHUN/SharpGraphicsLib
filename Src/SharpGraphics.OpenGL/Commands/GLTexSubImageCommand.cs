using SharpGraphics.Allocator;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using SharpGraphics.Utils;

namespace SharpGraphics.OpenGL.Commands
{

    public sealed class GLGenerateMipMapsCommand : IGLCommand
    {
        private readonly IGLTexture _texture;
        public GLGenerateMipMapsCommand(IGLTexture texture) => _texture = texture;
        public void Execute() => _texture.GLGenerateMipMaps();
        public override string ToString() => $"Generate MipMaps";
    }


    public sealed class GLTexSubImageCommand<T> : IGLCommand where T : unmanaged
    {

        private readonly IGLTexture _texture;
        private readonly IntPtr _data;
        private readonly CopyBufferTextureRange _range;

        public GLTexSubImageCommand(IGLTexture texture, IMemoryAllocator allocator, in Span<T> data, in CopyBufferTextureRange range)
        {
            _texture = texture;
            _data = allocator.AllocateThenCopy(data).address;
            _range = range;
        }
        public GLTexSubImageCommand(IGLTexture texture, IMemoryAllocator allocator, in ReadOnlySpan<T> data, in CopyBufferTextureRange range)
        {
            _texture = texture;
            _data = allocator.AllocateThenCopy(data).address;
            _range = range;
        }
        public GLTexSubImageCommand(IGLTexture texture, IMemoryAllocator allocator, in Memory<T> data, in CopyBufferTextureRange range)
        {
            _texture = texture;
            _data = allocator.AllocateThenCopy(data).address;
            _range = range;
        }
        public GLTexSubImageCommand(IGLTexture texture, IMemoryAllocator allocator, in ReadOnlyMemory<T> data, in CopyBufferTextureRange range)
        {
            _texture = texture;
            _data = allocator.AllocateThenCopy(data).address;
            _range = range;
        }

        public void Execute() => _texture.GLStoreData(_data, _range);

        public override string ToString() => $"Tex Sub Image {typeof(T).FullName} (Range: {_range})";

    }
    public sealed class GLTexSubImageMultipleRangesCommand<T> : IGLCommand where T : unmanaged
    {

        private readonly IGLTexture _texture;
        private readonly IntPtr _data;
        private readonly IntPtr _ranges;
        private readonly int _rangeCount;

        public GLTexSubImageMultipleRangesCommand(IGLTexture texture, IMemoryAllocator allocator, in Span<T> data, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            _texture = texture;
            _data = allocator.AllocateThenCopy(data).address;
            _ranges = allocator.AllocateThenCopy(ranges).address;
            _rangeCount = ranges.Length;
        }
        public GLTexSubImageMultipleRangesCommand(IGLTexture texture, IMemoryAllocator allocator, in ReadOnlySpan<T> data, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            _texture = texture;
            _data = allocator.AllocateThenCopy(data).address;
            _ranges = allocator.AllocateThenCopy(ranges).address;
            _rangeCount = ranges.Length;
        }
        public GLTexSubImageMultipleRangesCommand(IGLTexture texture, IMemoryAllocator allocator, in Memory<T> data, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            _texture = texture;
            _data = allocator.AllocateThenCopy(data).address;
            _ranges = allocator.AllocateThenCopy(ranges).address;
            _rangeCount = ranges.Length;
        }
        public GLTexSubImageMultipleRangesCommand(IGLTexture texture, IMemoryAllocator allocator, in ReadOnlyMemory<T> data, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            _texture = texture;
            _data = allocator.AllocateThenCopy(data).address;
            _ranges = allocator.AllocateThenCopy(ranges).address;
            _rangeCount = ranges.Length;
        }

        public unsafe void Execute() => _texture.GLStoreData(_data, new ReadOnlySpan<CopyBufferTextureRange>(_ranges.ToPointer(), _rangeCount));

        public override string ToString() => $"Tex Sub Image Multiple Ranges {typeof(T).FullName} (Ranges: {_rangeCount})";

    }

    internal sealed class GLTexSubImageFromEmulatedStagingBufferCommand : IGLCommand
    {

        private readonly IGLTexture _texture;
        private readonly IGLEmulatedStagingDataBuffer _stagingBuffer;
        private readonly CopyBufferTextureRange _range;

        internal GLTexSubImageFromEmulatedStagingBufferCommand(IGLTexture texture, IGLEmulatedStagingDataBuffer stagingBuffer, in CopyBufferTextureRange range)
        {
            _texture = texture;
            _stagingBuffer = stagingBuffer;
            _range = range;
        }

        public void Execute() => _texture.GLStoreData(_stagingBuffer.Pointer, _range);

        public override string ToString() => $"TexSubImage From Emulated Staging Buffer (Range: {_range})";

    }
    internal sealed class GLTexSubImageMultipleRangesFromEmulatedStagingBufferCommand : IGLCommand
    {

        private readonly IGLTexture _texture;
        private readonly IGLEmulatedStagingDataBuffer _stagingBuffer;
        private readonly MemoryAllocation _ranges;
        private readonly int _rangeCount;

        internal GLTexSubImageMultipleRangesFromEmulatedStagingBufferCommand(IGLTexture texture, IMemoryAllocator allocator, IGLEmulatedStagingDataBuffer stagingBuffer, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            _texture = texture;
            _stagingBuffer = stagingBuffer;
            _ranges = allocator.AllocateThenCopy(ranges);
            _rangeCount = ranges.Length;
        }

        public void Execute() => _texture.GLStoreData(_stagingBuffer.Pointer, _ranges.ReadOnlySpan<CopyBufferTextureRange>(_rangeCount));

        public override string ToString() => $"TexSubImage Multiple Ranges From Emulated Staging Buffer (Ranges: {_rangeCount})";

    }

    internal sealed class GLTexSubImageFromUnPackBufferCommand : IGLCommand
    {

        private readonly IGLTexture _texture;
        private readonly IGLDataBuffer _dataBuffer;
        private readonly CopyBufferTextureRange _range;

        internal GLTexSubImageFromUnPackBufferCommand(IGLTexture texture, IGLDataBuffer dataBuffer, in CopyBufferTextureRange range)
        {
            _texture = texture;
            _dataBuffer = dataBuffer;
            _range = range;
        }

        public void Execute()
        {
            _dataBuffer.GLBindUnPackBuffer();
            _texture.GLStoreData(IntPtr.Zero, _range);
        }

        public override string ToString() => $"TexSubImage From UnPack Buffer (Range: {_range})";

    }
    internal sealed class GLTexSubImageMultipleRangesFromUnPackBufferCommand : IGLCommand
    {

        private readonly IGLTexture _texture;
        private readonly IGLDataBuffer _dataBuffer;
        private readonly MemoryAllocation _ranges;
        private readonly int _rangeCount;

        internal GLTexSubImageMultipleRangesFromUnPackBufferCommand(IGLTexture texture, IMemoryAllocator allocator, IGLDataBuffer dataBuffer, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            _texture = texture;
            _dataBuffer = dataBuffer;
            _ranges = allocator.AllocateThenCopy(ranges);
            _rangeCount = ranges.Length;
        }

        public void Execute()
        {
            _dataBuffer.GLBindUnPackBuffer();
            _texture.GLStoreData(IntPtr.Zero, _ranges.ReadOnlySpan<CopyBufferTextureRange>(_rangeCount));
        }

        public override string ToString() => $"TexSubImage Multiple Ranges From UnPack Buffer (Ranges: {_rangeCount})";

    }

}
