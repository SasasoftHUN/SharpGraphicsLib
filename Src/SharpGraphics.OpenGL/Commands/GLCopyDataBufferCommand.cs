using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.Commands
{
    internal sealed class GLCopyDataBufferCommand : IGLCommand
    {

        private readonly IGLDataBuffer _source;
        private readonly IGLDataBuffer _destination;
        private readonly IntPtr _size;
        private readonly IntPtr _sourceOffset;
        private readonly IntPtr _destinationOffset;

        public GLCopyDataBufferCommand(IGLDataBuffer source, IGLDataBuffer destination, IntPtr size, IntPtr sourceOffset, IntPtr destinationOffset)
        {
            this._source = source;
            this._destination = destination;
            this._size = size;
            this._sourceOffset = sourceOffset;
            this._destinationOffset = destinationOffset;
        }

        public void Execute() => _source.GLCopyTo(_destination, _size, _sourceOffset, _destinationOffset);

        public override string ToString() => $"Buffer Copy To (Size: {(int)_size}, Source Offset: {(int)_sourceOffset}, Destination Offset: {(int)_destinationOffset})";

    }

    internal sealed class GLCopyEmulatedStagingBufferCommand : IGLCommand
    {

        private readonly IntPtr _sourcePointer;
        private readonly IntPtr _destinationPointer;
        private readonly ulong _size;

        internal GLCopyEmulatedStagingBufferCommand(IntPtr sourcePointer, IntPtr destinationPointer, ulong size)
        {
            _sourcePointer = sourcePointer;
            _destinationPointer = destinationPointer;
            _size = size;
        }

        public unsafe void Execute() => Buffer.MemoryCopy(_sourcePointer.ToPointer(), _destinationPointer.ToPointer(), _size, _size);

        public override string ToString() => $"Copy To Staging From Staging (Size: {_size})";

    }

}
