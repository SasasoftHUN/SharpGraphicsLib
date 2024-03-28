using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.Commands
{
    internal sealed class GLBindFrameBufferCommand : IGLCommand
    {

        private readonly IGLFrameBuffer _framebuffer;
        private readonly int _index;

        internal GLBindFrameBufferCommand(IGLFrameBuffer framebuffer, int index)
        {
            _framebuffer = framebuffer;
            _index = index;
        }

        public void Execute() => _framebuffer.GLBind(_index);

        public override string ToString() => $"Bind FBO (Index: {_index})";

    }

    internal sealed class GLBlitFrameBufferFromDefault : IGLCommand
    {

        private readonly IGLFrameBuffer _framebuffer;

        internal GLBlitFrameBufferFromDefault(IGLFrameBuffer framebuffer) => _framebuffer = framebuffer;

        public void Execute() => _framebuffer.GLBlitFromDefault();

        public override string ToString() => "Blit into FBO from Default FBO";

    }
    internal sealed class GLBlitFrameBufferToDefault : IGLCommand
    {

        private readonly IGLFrameBuffer _framebuffer;

        internal GLBlitFrameBufferToDefault(IGLFrameBuffer framebuffer) => _framebuffer = framebuffer;

        public void Execute() => _framebuffer.GLBlitToDefault();

        public override string ToString() => "Blit FBO into Default FBO";

    }

}
