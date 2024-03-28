using SharpGraphics.OpenGL.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.Commands
{
    internal sealed class GLBindUniformBufferCommand : IGLCommand
    {

        private readonly uint _index;
        private readonly IGLDataBuffer _uniformBuffer;

        internal GLBindUniformBufferCommand(uint index, IGLDataBuffer uniformBuffer)
        {
            _index = index;
            _uniformBuffer = uniformBuffer;
        }

        public void Execute() => _uniformBuffer.GLBindUniformBuffer(_index);

        public override string ToString() => $"Bind Uniform Buffer (Index: {_index})";

    }

    internal sealed class GLBindUniformDynamicBufferCommand : IGLCommand
    {

        private readonly uint _index;
        private readonly IGLDataBuffer _uniformBuffer;
        private readonly IntPtr _offset;
        private readonly IntPtr _size;

        internal GLBindUniformDynamicBufferCommand(uint index, IGLDataBuffer uniformBuffer, uint elementOffset, int dataIndex)
        {
            _index = index;
            _uniformBuffer = uniformBuffer;
            _offset = new IntPtr(elementOffset * dataIndex);
            _size = new IntPtr(elementOffset);
        }

        public void Execute() => _uniformBuffer.GLBindUniformBuffer(_index, _offset, _size);

        public override string ToString() => $"Bind Uniform Buffer (Index: {_index}, Offset: {(int)_offset}, Size: {(int)_size})";

    }
}
