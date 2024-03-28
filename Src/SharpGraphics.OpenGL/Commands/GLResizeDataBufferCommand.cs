using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.Commands
{
    internal sealed class GLResizeDataBufferCommand : IGLCommand
    {

        private readonly IGLDataBuffer _dataBuffer;
        private readonly int _size;

        public GLResizeDataBufferCommand(IGLDataBuffer dataBuffer, int size)
        {
            _dataBuffer = dataBuffer;
            _size = size;
        }

        public void Execute() => _dataBuffer.GLResize(_size);

        public override string ToString() => $"GL Resize DataBuffer (Size: {_size})";

    }
}
