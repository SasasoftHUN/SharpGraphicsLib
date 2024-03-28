using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.Commands
{

    internal sealed class GLMapMemoryCommand : GLCommandWithResult<IntPtr>
    {

        private readonly IGLDataBuffer _dataBuffer;
        private readonly IntPtr _offset;
        private readonly IntPtr _length;

        internal GLMapMemoryCommand(IGLDataBuffer dataBuffer, IntPtr offset, IntPtr length)
        {
            _dataBuffer = dataBuffer;
            _offset = offset;
            _length = length;
        }

        public override void Execute()
        {
            Result = _dataBuffer.GLMapMemory(_offset, _length);
            base.Execute();
        }

        public override string ToString() => $"Map Memory (Offset: {(int)_offset}, Size: {(int)_length})";

    }

    internal sealed class GLFlushMappedSystemMemoryCommand : IGLCommand
    {
        private readonly IGLDataBuffer _dataBuffer;
        private readonly IntPtr _offset;
        private readonly IntPtr _length;

        internal GLFlushMappedSystemMemoryCommand(IGLDataBuffer dataBuffer, IntPtr offset, IntPtr length)
        {
            _dataBuffer = dataBuffer;
            _offset = offset;
            _length = length;
        }

        public void Execute() => _dataBuffer.GLFlushMappedSystemMemory(_offset, _length);

        public override string ToString() => $"Flush Mapped System Memory (Offset: {(int)_offset}, Size: {(int)_length})";

    }

    internal sealed class GLUnMapMemoryCommand : IGLCommand
    {
        private readonly IGLDataBuffer _dataBuffer;

        internal GLUnMapMemoryCommand(IGLDataBuffer dataBuffer)
            => _dataBuffer = dataBuffer;

        public void Execute() => _dataBuffer.GLUnMapMemory();

        public override string ToString() => "UnMap Memory";

    }

}
