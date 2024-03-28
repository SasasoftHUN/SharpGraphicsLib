using System;
using System.Collections.Generic;
using System.Text;
using SharpGraphics.Utils;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using SharpGraphics.OpenGL.Commands;

namespace SharpGraphics.OpenGL.OpenGLCore.Commands
{

    internal sealed class GLCoreViewportScissorCommand : IGLCommand
    {

        private readonly RectInt _rect;
        private readonly bool _enableScissor;

        internal GLCoreViewportScissorCommand(in RectInt rect, bool enableScissor)
        {
            _rect = rect;
            _enableScissor = enableScissor;
        }

        public void Execute()
        {
            GL.Viewport(_rect.bottomLeft.x, _rect.bottomLeft.y, _rect.size.x, _rect.size.y);
            GL.Scissor(_rect.bottomLeft.x, _rect.bottomLeft.y, _rect.size.x, _rect.size.y);
            if (_enableScissor)
                GL.Enable(EnableCap.ScissorTest);
        }

        public override string ToString() => $"Viewport-Scissor ({_rect})";

    }

    internal sealed class GLCoreViewportCommand : IGLCommand
    {

        private readonly RectInt _rect;

        internal GLCoreViewportCommand(in RectInt rect) => _rect = rect;

        public void Execute() => GL.Viewport(_rect.bottomLeft.x, _rect.bottomLeft.y, _rect.size.x, _rect.size.y);

        public override string ToString() => $"Viewport ({_rect})";

    }

    internal sealed class GLCoreScissorCommand : IGLCommand
    {

        private readonly RectInt _rect;
        private readonly bool _enableScissor;

        internal GLCoreScissorCommand(in RectInt rect, bool enableScissor)
        {
            _rect = rect;
            _enableScissor = enableScissor;
        }

        public void Execute()
        {
            GL.Scissor(_rect.bottomLeft.x, _rect.bottomLeft.y, _rect.size.x, _rect.size.y);
            if (_enableScissor)
                GL.Enable(EnableCap.ScissorTest);
        }

        public override string ToString() => $"Scissor ({_rect})";

    }

}
