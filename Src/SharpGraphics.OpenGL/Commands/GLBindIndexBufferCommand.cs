using SharpGraphics.OpenGL.Commands;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.Commands
{
    internal sealed class GLBindIndexBufferCommand : IGLCommand
    {

        private readonly IGLDataBuffer _indexBuffer;

        internal GLBindIndexBufferCommand(IGLDataBuffer indexBuffer) => _indexBuffer = indexBuffer;

        public void Execute() => _indexBuffer.GLBindIndexBuffer();

        public override string ToString() => $"Bind Index Buffer (ID: {_indexBuffer.ID})";

    }
}
