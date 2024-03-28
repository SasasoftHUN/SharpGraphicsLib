using SharpGraphics.Allocator;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using SharpGraphics.Utils;

namespace SharpGraphics.OpenGL.Commands
{

    internal sealed class GLGetTexSubImageToEmulatedStagingBufferCommand : IGLCommand
    {

        private readonly IGLTexture _texture;
        private readonly IGLEmulatedStagingDataBuffer _stagingBuffer;
        private readonly CopyBufferTextureRange _range;

        internal GLGetTexSubImageToEmulatedStagingBufferCommand(IGLTexture texture, IGLEmulatedStagingDataBuffer stagingBuffer, in CopyBufferTextureRange range)
        {
            _texture = texture;
            _stagingBuffer = stagingBuffer;
            _range = range;
        }

        public void Execute() => _texture.GLReadData(_stagingBuffer.Pointer, _range);

        public override string ToString() => $"GetTexSubImage To Emulated Staging Buffer (Range: {_range})";

    }
    internal sealed class GLGetTexSubImageMultipleRangesToEmulatedStagingBufferCommand : IGLCommand
    {

        private readonly IGLTexture _texture;
        private readonly IGLEmulatedStagingDataBuffer _stagingBuffer;
        private readonly MemoryAllocation _ranges;
        private readonly int _rangeCount;

        internal GLGetTexSubImageMultipleRangesToEmulatedStagingBufferCommand(IGLTexture texture, IMemoryAllocator allocator, IGLEmulatedStagingDataBuffer stagingBuffer, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            _texture = texture;
            _stagingBuffer = stagingBuffer;
            _ranges = allocator.AllocateThenCopy(ranges);
            _rangeCount = ranges.Length;
        }

        public void Execute() => _texture.GLReadData(_stagingBuffer.Pointer, _ranges.ReadOnlySpan<CopyBufferTextureRange>(_rangeCount));

        public override string ToString() => $"GetTexSubImage Multiple Ranges To Emulated Staging Buffer (Ranges: {_rangeCount})";

    }

    internal sealed class GLGetTexSubImageToPackBufferCommand : IGLCommand
    {

        private readonly IGLTexture _texture;
        private readonly IGLDataBuffer _dataBuffer;
        private readonly CopyBufferTextureRange _range;

        internal GLGetTexSubImageToPackBufferCommand(IGLTexture texture, IGLDataBuffer dataBuffer, in CopyBufferTextureRange range)
        {
            _texture = texture;
            _dataBuffer = dataBuffer;
            _range = range;
        }

        public void Execute()
        {
            _dataBuffer.GLBindPackBuffer();
            _texture.GLReadData(IntPtr.Zero, _range);
        }

        public override string ToString() => $"GetTexSubImage To Pack Buffer (Range: {_range})";

    }
    internal sealed class GLGetTexSubImageMultipleRangesToPackBufferCommand : IGLCommand
    {

        private readonly IGLTexture _texture;
        private readonly IGLDataBuffer _dataBuffer;
        private readonly MemoryAllocation _ranges;
        private readonly int _rangeCount;

        internal GLGetTexSubImageMultipleRangesToPackBufferCommand(IGLTexture texture, IMemoryAllocator allocator, IGLDataBuffer dataBuffer, in ReadOnlySpan<CopyBufferTextureRange> ranges)
        {
            _texture = texture;
            _dataBuffer = dataBuffer;
            _ranges = allocator.AllocateThenCopy(ranges);
            _rangeCount = ranges.Length;
        }

        public void Execute()
        {
            _dataBuffer.GLBindPackBuffer();
            _texture.GLReadData(IntPtr.Zero, _ranges.ReadOnlySpan<CopyBufferTextureRange>(_rangeCount));
        }

        public override string ToString() => $"GetTexSubImage Multiple Ranges To Pack Buffer (Ranges: {_rangeCount})";

    }

}
