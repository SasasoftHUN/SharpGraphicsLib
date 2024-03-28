using SharpGraphics.OpenGL.Contexts;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.Commands
{
    internal sealed class GLSwapBuffersCommand : IGLCommand
    {

        private readonly IGLContext _context;

        internal GLSwapBuffersCommand(IGLContext context) => _context = context;

        public void Execute() => _context.SwapBuffers();

        public override string ToString() => "Swap Buffers";

    }
}
