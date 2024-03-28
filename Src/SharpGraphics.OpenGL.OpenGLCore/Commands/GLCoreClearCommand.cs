using System;
using System.Collections.Generic;
using System.Text;
using SharpGraphics.Utils;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using SharpGraphics.Allocator;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using SharpGraphics.OpenGL.CommandBuffers;
#if OPENTK4
using OpenTK.Mathematics;
#endif
using SharpGraphics.OpenGL.Commands;

namespace SharpGraphics.OpenGL.OpenGLCore.Commands
{
    internal sealed class GLCoreClearCommand : IGLCommand
    {

        private readonly Vector2UInt _size;
        private readonly IntPtr _clearColorsPointer;
        private readonly int _clearColorCount;
        private readonly float? _clearDepth;
        private readonly int? _clearStencil;

        internal GLCoreClearCommand(in Vector2UInt size, in ReadOnlySpan<GLClearColor> clearColors, float? clearDepth, int? clearStencil, IMemoryAllocator allocator)
        {
            _size = size;
            if (clearColors.Length > 0)
            {
                MemoryAllocation allocation = allocator.AllocateThenCopy(clearColors);
                _clearColorsPointer = allocation.address;
                _clearColorCount = clearColors.Length;
            }
            else
            {
                _clearColorsPointer = IntPtr.Zero;
                _clearColorCount = 0;
            }
            _clearDepth = clearDepth;
            _clearStencil = clearStencil;
        }

        public void Execute()
        {
            GL.Viewport(0, 0, (int)_size.x, (int)_size.y);

            //Clear Colors
            if (_clearColorCount > 0)
            {
                bool restoreColorMask = false;
                Span<bool> colorMask = stackalloc bool[4];
                Span<GLClearColor> clearColors;
                unsafe
                {
                    fixed (bool* p = &colorMask[0])
                    {
                        GL.GetBoolean(GetPName.ColorWritemask, p);
                        if (!colorMask[0] || !colorMask[1] || !colorMask[2] || !colorMask[3])
                        {
                            restoreColorMask = true;
                            GL.ColorMask(true, true, true, true);
                        }
                    }
                    clearColors = new Span<GLClearColor>(_clearColorsPointer.ToPointer(), _clearColorCount);
                }

                for (int i = 0; i < clearColors.Length; i++)
                    GL.ClearBuffer(ClearBuffer.Color, clearColors[i].buffer, ref clearColors[i].color.X);

                if (restoreColorMask)
                    GL.ColorMask(colorMask[0], colorMask[1], colorMask[2], colorMask[3]);
            }

            //Clear Depth
            if (_clearDepth.HasValue)
            {
                bool disableDepthMask = false;
                GL.GetBoolean(GetPName.DepthWritemask, out bool dm);
                if (!dm)
                {
                    GL.DepthMask(true);
                    disableDepthMask = true;
                }

                float depth = _clearDepth.Value;
                GL.ClearBuffer(ClearBuffer.Depth, 0, ref depth);

                if (disableDepthMask)
                    GL.DepthMask(false);

            }

            //Clear Stencil
            int? stencilMask = default;
            if (_clearStencil.HasValue)
            {
                GL.GetInteger(GetPName.StencilWritemask, out int sm);
                if (sm != 0xFF)
                {
                    GL.StencilMask(0xFF);
                    stencilMask = sm;
                }

                int stencil = _clearStencil.Value;
                GL.ClearBuffer(ClearBuffer.Stencil, 0, ref stencil);

                if (stencilMask.HasValue)
                    GL.StencilMask(stencilMask.Value);
            }
        }

        public override string ToString() => $"Clear (Size: {_size})";

    }
}
