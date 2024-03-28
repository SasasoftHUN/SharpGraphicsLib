using SharpGraphics.OpenGL.CommandBuffers;
using SharpGraphics.OpenGL.Commands;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.OpenGL
{
    public static class GLUtils
    {

        public static void ToSwapInterval(this PresentMode presentMode, out int interval, out int buffers)
        {
            switch (presentMode)
            {
                case PresentMode.Immediate: interval = 0; buffers = 2; break;
                case PresentMode.VSyncDoubleBuffer: interval = 1; buffers = 2; break;
                case PresentMode.AdaptiveDoubleBuffer: interval = -1; buffers = 2; break;

                case PresentMode.VSyncTripleBuffer:
                default:
                    interval = 1;
                    buffers = 3;
                    break;
            }
        }

        public static void ToColorFormat(this DataFormat format, out int colorBits, out int redBits, out int greenBits, out int blueBits, out int alphaBits)
        {
            switch (format)
            {
                //TODO: "Parse" formats
                default: colorBits = 32; redBits = 8; greenBits = 8; blueBits = 8; alphaBits = 8; break;
            }
        }
        public static void ToDepthStencilFormat(this DataFormat format, out int depth, out int stencil)
        {
            switch (format)
            {
                case DataFormat.Depth16un: depth = 16; stencil = 0; break;
                case DataFormat.Depth24un: depth = 24; stencil = 0; break;
                case DataFormat.Depth32f: depth = 32; stencil = 0; break;
                case DataFormat.Stencil8ui: depth = 0; stencil = 8; break;
                case DataFormat.Depth16un_Stencil8ui: depth = 16; stencil = 8; break;
                case DataFormat.Depth24un_Stencil8ui: depth = 24; stencil = 8; break;
                case DataFormat.Depth32f_Stencil8ui: depth = 32; stencil = 8; break;

                case DataFormat.Undefined:
                default: depth = 0; stencil = 0; break;
            }
        }
        public static DataFormat ToDataFormat(this int depth, int stencil)
            => depth switch
            {
                32 => stencil switch
                {
                    8 => DataFormat.Depth32f_Stencil8ui,
                    0 => DataFormat.Depth32f,
                    _ => DataFormat.Undefined,
                },
                24 => stencil switch
                {
                    8 => DataFormat.Depth24un_Stencil8ui,
                    0 => DataFormat.Depth24un,
                    _ => DataFormat.Undefined,
                },
                16 => stencil switch
                {
                    8 => DataFormat.Depth16un_Stencil8ui,
                    0 => DataFormat.Depth16un,
                    _ => DataFormat.Undefined,
                },
                0 => stencil switch
                {
                    8 => DataFormat.Stencil8ui,
                    _ => DataFormat.Undefined,
                },
                _ => DataFormat.Undefined,
            };

        public static DataBufferType MakeBufferTypeTrivial(this DataBufferType type)
        {
            if (type.HasFlag(DataBufferType.Read))
                type |= DataBufferType.CopySource;
            if (type.HasFlag(DataBufferType.Store))
                type |= DataBufferType.CopyDestination;
            
            return type;
        }

        public static int ToSwizzleInt(this TextureSwizzleType type, TextureSwizzleType original)
            => type switch
            {
                TextureSwizzleType.Original => original.ToSwizzleInt(original),
                TextureSwizzleType.Zero => 0,
                TextureSwizzleType.One => 1,
                TextureSwizzleType.Red => 6403,
                TextureSwizzleType.Green => 6404,
                TextureSwizzleType.Blue => 6405,
                TextureSwizzleType.Alpha => 6406,
                _ => original.ToSwizzleInt(original),
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRenderBuffer(this TextureType textureType, in MemoryType memoryType, uint mipLevels)
            => memoryType.IsDeviceOnly && mipLevels == 0u && textureType switch
            {
                TextureType.ColorAttachment => true,
                TextureType.DepthStencilAttachment => true,
                _ => false,
            };

    }
}
