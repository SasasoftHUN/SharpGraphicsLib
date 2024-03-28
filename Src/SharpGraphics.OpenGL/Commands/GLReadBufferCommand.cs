using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.Commands
{
    internal sealed class GLReadBufferIntoEmulatedStagingBufferCommand : IGLCommand
    {

        private readonly IGLDataBuffer _dataBuffer;
        private readonly IntPtr _offset;
        private readonly IGLEmulatedStagingDataBuffer _stagingBuffer;
        private readonly int _stagingBufferOffset;
        private readonly IntPtr _size;

        internal GLReadBufferIntoEmulatedStagingBufferCommand(IGLDataBuffer dataBuffer, IntPtr offset, IGLEmulatedStagingDataBuffer stagingBuffer, int stagingBufferOffset, int size)
        {
            _dataBuffer = dataBuffer;
            _offset = offset;
            _stagingBuffer = stagingBuffer;
            _stagingBufferOffset = stagingBufferOffset;
            _size = new IntPtr(size);
        }

        public void Execute() => _dataBuffer.GLReadData(_offset, _size, _stagingBuffer.Pointer + _stagingBufferOffset);

        public override string ToString() => $"Buffer Data From Emulated Staging Buffer (Size: {_size})";

    }
}
