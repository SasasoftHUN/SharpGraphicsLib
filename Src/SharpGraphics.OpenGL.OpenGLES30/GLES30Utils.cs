using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES30;
using SharpGraphics.Shaders;
#if OPENTK4
using OpenTK.Mathematics;
#endif

namespace SharpGraphics.OpenGL.OpenGLES30
{
    internal static class GLES30Utils
    {

#if ANDROID
        [DllImport("libGLESv3", EntryPoint = "glGetStringi", ExactSpelling = true)]
        [System.Security.SuppressUnmanagedCodeSecurity]
        internal static extern IntPtr GetStringi(StringNameIndexed name, int index);
#endif


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToOpenTK(this System.Numerics.Vector2 v) => new Vector2(v.X, v.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToOpenTK(this System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 ToOpenTK(this System.Numerics.Vector4 v) => new Vector4(v.X, v.Y, v.Z, v.W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color4 ToOpenTK(this System.Drawing.Color v) => new Color4(v.R, v.G, v.B, v.A);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color4 ToOpenTKColor(this System.Numerics.Vector4 v) => new Color4(v.X, v.Y, v.Z, v.W);


        public static ShaderType ToShaderType(this GraphicsShaderStages shader)
            => shader switch
            {
                GraphicsShaderStages.Vertex => ShaderType.VertexShader,
                //TODO: Implement for Geometry Shader, no support though... emulation somehow in the future?
                //GraphicsShaderStages.Geometry => throw new NotSupportedException($"{shader} not supported in OpenGL ES 3.0!");
                //TODO: Implement for Tessellation Shader, no support though... emulation somehow in the future?
                //GraphicsShaderStages.TessellationControl => throw new NotSupportedException($"{shader} not supported in OpenGL ES 3.0!");
                //GraphicsShaderStages.TessellationEvaluation => throw new NotSupportedException($"{shader} not supported in OpenGL ES 3.0!");
                GraphicsShaderStages.Fragment => ShaderType.FragmentShader,
                _ => ShaderType.VertexShader,
            };

#if ANDROID
        public static BeginMode ToPrimitiveType(this GeometryType type)
            => type switch
            {
                GeometryType.Points => BeginMode.Points,

                GeometryType.Lines => BeginMode.Lines,
                //No LineLoop in Vulkan
                GeometryType.LineStrip => BeginMode.LineStrip,

                GeometryType.Triangles => BeginMode.Triangles,
                GeometryType.TriangleStrip => BeginMode.TriangleStrip,
                GeometryType.TriangleFan => BeginMode.TriangleFan,

                //Quads, Polygon... Nope...

                //TODO: Implement for Geometry Shader, no support though... emulation somehow in the future?
                //GeometryType.LinesAdjacency => throw new NotSupportedException($"{type} not supported in OpenGL ES 3.0!");
                //GeometryType.LineStripAdjacency => throw new NotSupportedException($"{type} not supported in OpenGL ES 3.0!");
                //GeometryType.TrianglesAdjacency => throw new NotSupportedException($"{type} not supported in OpenGL ES 3.0!");
                //GeometryType.TriangleStripAdjacency => throw new NotSupportedException($"{type} not supported in OpenGL ES 3.0!");

                //TODO: Implement for Tessellation Shader, no support though... emulation somehow in the future?
                //GeometryType.Patches =>  => throw new NotSupportedException($"{type} not supported in OpenGL ES 3.0!");

                _ => BeginMode.Triangles,
            };
#else
        public static PrimitiveType ToPrimitiveType(this GeometryType type)
            => type switch
            {
                GeometryType.Points => PrimitiveType.Points,

                GeometryType.Lines => PrimitiveType.Lines,
                //No LineLoop in Vulkan
                GeometryType.LineStrip => PrimitiveType.LineStrip,

                GeometryType.Triangles => PrimitiveType.Triangles,
                GeometryType.TriangleStrip => PrimitiveType.TriangleStrip,
                GeometryType.TriangleFan => PrimitiveType.TriangleFan,

                //Quads, Polygon... Nope...

                //TODO: Implement for Geometry Shader
                //GeometryType.LinesAdjacency => PrimitiveType.LinesAdjacency,
                //GeometryType.LineStripAdjacency => PrimitiveType.LineStripAdjacency,
                //GeometryType.TrianglesAdjacency => PrimitiveType.TrianglesAdjacency,
                //GeometryType.TriangleStripAdjacency => PrimitiveType.TriangleStripAdjacency,

                //TODO: Implement for Tessellation Shader
                //GeometryType.Patches => PrimitiveType.Patches, //When to use PatchesExt?

                _ => PrimitiveType.Triangles,
            };
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DrawElementsType ToDrawElementsType(this GraphicsCommandBuffer.IndexType indexType)
            => indexType switch
            {
                GraphicsCommandBuffer.IndexType.UnsignedShort => DrawElementsType.UnsignedShort,
                GraphicsCommandBuffer.IndexType.UnsignedInt => DrawElementsType.UnsignedInt,
                _ => DrawElementsType.UnsignedShort,
            };


#if ANDROID
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferUsage ToBufferUsage(this DataBufferType type, MappableMemoryType? memoryType)
            => memoryType.HasValue ? type.ToBufferUsage(memoryType.Value) : type.ToBufferUsage();
        public static BufferUsage ToBufferUsage(this DataBufferType type) //Device Only
        {
            //Draw
            if (type.HasFlag(DataBufferType.Store))
            {
                if (type.HasFlag(DataBufferType.UniformData)) return BufferUsage.DynamicDraw;
                return BufferUsage.StaticDraw;
            }

            //Read
            if (type.HasFlag(DataBufferType.Read))
            {
                return BufferUsage.DynamicDraw;
            }

            //Copy
            if (type.HasFlag(DataBufferType.CopySource) || type.HasFlag(DataBufferType.CopyDestination)) return BufferUsage.StaticDraw;

            return BufferUsage.StaticDraw;
        }
        public static BufferUsage ToBufferUsage(this DataBufferType type, MappableMemoryType memoryType) //Mappable
        {
            //Draw
            if (type.HasFlag(DataBufferType.Store))
            {
                if (type.HasFlag(DataBufferType.UniformData)) return BufferUsage.StreamDraw;
                return BufferUsage.DynamicDraw;
            }
            //Read
            if (type.HasFlag(DataBufferType.Read))
            {
                return BufferUsage.StreamDraw;
            }

            //Draw - There is only Draw...
            if (memoryType == MappableMemoryType.DontCare)
                return BufferUsage.DynamicDraw;
            else if (memoryType.HasFlag(MappableMemoryType.DeviceLocal))
                return memoryType.HasFlag(MappableMemoryType.Coherent) || memoryType.HasFlag(MappableMemoryType.Cached) ? BufferUsage.StreamDraw : BufferUsage.DynamicDraw;
            else return BufferUsage.StreamDraw;
        }
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferUsageHint ToBufferUsageHint(this DataBufferType type, MappableMemoryType? memoryType)
            => memoryType.HasValue ? type.ToBufferUsageHint(memoryType.Value) : type.ToBufferUsageHint();
        public static BufferUsageHint ToBufferUsageHint(this DataBufferType type) //Device Only
        {
            //Draw
            if (type.HasFlag(DataBufferType.Store))
            {
                if (type.HasFlag(DataBufferType.UniformData)) return BufferUsageHint.DynamicDraw;
                return BufferUsageHint.StaticDraw;
            }

            //Read
            if (type.HasFlag(DataBufferType.Read))
            {
                return BufferUsageHint.DynamicRead;
            }

            //Copy
            if (type.HasFlag(DataBufferType.CopySource) || type.HasFlag(DataBufferType.CopyDestination)) return BufferUsageHint.StaticCopy;

            return BufferUsageHint.StaticDraw;
        }
        public static BufferUsageHint ToBufferUsageHint(this DataBufferType type, MappableMemoryType memoryType) //Mappable
        {
            //Draw
            if (type.HasFlag(DataBufferType.Store))
            {
                if (type.HasFlag(DataBufferType.UniformData)) return BufferUsageHint.StreamDraw;
                return BufferUsageHint.DynamicDraw;
            }
            //Read
            if (type.HasFlag(DataBufferType.Read))
            {
                return BufferUsageHint.StreamRead;
            }

            //Draw
            if (type.HasFlag(DataBufferType.VertexData) || type.HasFlag(DataBufferType.IndexData) || type.HasFlag(DataBufferType.UniformData))
            {
                if (memoryType == MappableMemoryType.DontCare)
                    return BufferUsageHint.DynamicDraw;
                else if (memoryType.HasFlag(MappableMemoryType.DeviceLocal))
                    return memoryType.HasFlag(MappableMemoryType.Coherent) || memoryType.HasFlag(MappableMemoryType.Cached) ? BufferUsageHint.StreamDraw : BufferUsageHint.DynamicDraw;
                else return BufferUsageHint.StreamDraw;
            }

            //Copy
            if (type.HasFlag(DataBufferType.CopySource) || type.HasFlag(DataBufferType.CopyDestination))
            {
                if (memoryType == MappableMemoryType.DontCare)
                    return BufferUsageHint.DynamicCopy;
                else if (memoryType.HasFlag(MappableMemoryType.DeviceLocal))
                    return memoryType.HasFlag(MappableMemoryType.Coherent) || memoryType.HasFlag(MappableMemoryType.Cached) ? BufferUsageHint.StreamCopy : BufferUsageHint.DynamicCopy;
                else return BufferUsageHint.StreamCopy;
            }

            return BufferUsageHint.DynamicDraw;
        }
#endif

        public static DepthFunction ToDepthFunction(this ComparisonType comparison)
            => comparison switch
            {
                ComparisonType.Never => DepthFunction.Never,
                ComparisonType.Less => DepthFunction.Less,
                ComparisonType.Equal => DepthFunction.Equal,
                ComparisonType.LessOrEqual => DepthFunction.Lequal,
                ComparisonType.Greater => DepthFunction.Greater,
                ComparisonType.NotEqual => DepthFunction.Notequal,
                ComparisonType.GreaterOrEqual => DepthFunction.Gequal,
                ComparisonType.Always => DepthFunction.Always,
                _ => DepthFunction.Never,
            };

#if !OPENTK4
        public static ColorFormat ToColorFormat(this DataFormat format)
            => format switch
            {
                DataFormat.Undefined => DisplayDevice.Default.BitsPerPixel,

                DataFormat.RGB8un => new ColorFormat(8, 8, 8, 0),
                DataFormat.RGB8n => new ColorFormat(8, 8, 8, 0),
                DataFormat.RGB8us => new ColorFormat(8, 8, 8, 0),
                DataFormat.RGB8s => new ColorFormat(8, 8, 8, 0),
                DataFormat.RGB8ui => new ColorFormat(8, 8, 8, 0),
                DataFormat.RGB8i => new ColorFormat(8, 8, 8, 0),
                DataFormat.RGB8srgb => new ColorFormat(8, 8, 8, 0),
                DataFormat.BGR8un => new ColorFormat(8, 8, 8, 0),
                DataFormat.BGR8n => new ColorFormat(8, 8, 8, 0),
                DataFormat.BGR8us => new ColorFormat(8, 8, 8, 0),
                DataFormat.BGR8s => new ColorFormat(8, 8, 8, 0),
                DataFormat.BGR8ui => new ColorFormat(8, 8, 8, 0),
                DataFormat.BGR8i => new ColorFormat(8, 8, 8, 0),
                DataFormat.BGR8srgb => new ColorFormat(8, 8, 8, 0),

                DataFormat.RGBA8un => new ColorFormat(8, 8, 8, 8),
                DataFormat.RGBA8n => new ColorFormat(8, 8, 8, 8),
                DataFormat.RGBA8us => new ColorFormat(8, 8, 8, 8),
                DataFormat.RGBA8s => new ColorFormat(8, 8, 8, 8),
                DataFormat.RGBA8ui => new ColorFormat(8, 8, 8, 8),
                DataFormat.RGBA8i => new ColorFormat(8, 8, 8, 8),
                DataFormat.RGBA8srgb => new ColorFormat(8, 8, 8, 8),
                DataFormat.BGRA8un => new ColorFormat(8, 8, 8, 8),
                DataFormat.BGRA8n => new ColorFormat(8, 8, 8, 8),
                DataFormat.BGRA8us => new ColorFormat(8, 8, 8, 8),
                DataFormat.BGRA8s => new ColorFormat(8, 8, 8, 8),
                DataFormat.BGRA8ui => new ColorFormat(8, 8, 8, 8),
                DataFormat.BGRA8i => new ColorFormat(8, 8, 8, 8),
                DataFormat.BGRA8srgb => new ColorFormat(8, 8, 8, 8),


                DataFormat.RGB16un => new ColorFormat(16, 16, 16, 0),
                DataFormat.RGB16n => new ColorFormat(16, 16, 16, 0),
                DataFormat.RGB16us => new ColorFormat(16, 16, 16, 0),
                DataFormat.RGB16s => new ColorFormat(16, 16, 16, 0),
                DataFormat.RGB16ui => new ColorFormat(16, 16, 16, 0),
                DataFormat.RGB16i => new ColorFormat(16, 16, 16, 0),
                DataFormat.RGB16f => new ColorFormat(16, 16, 16, 0),

                DataFormat.RGBA16un => new ColorFormat(16, 16, 16, 16),
                DataFormat.RGBA16n => new ColorFormat(16, 16, 16, 16),
                DataFormat.RGBA16us => new ColorFormat(16, 16, 16, 16),
                DataFormat.RGBA16s => new ColorFormat(16, 16, 16, 16),
                DataFormat.RGBA16ui => new ColorFormat(16, 16, 16, 16),
                DataFormat.RGBA16i => new ColorFormat(16, 16, 16, 16),
                DataFormat.RGBA16f => new ColorFormat(16, 16, 16, 16),


                DataFormat.RGB32ui => new ColorFormat(32, 32, 32, 0),
                DataFormat.RGB32i => new ColorFormat(32, 32, 32, 0),
                DataFormat.RGB32f => new ColorFormat(32, 32, 32, 0),

                DataFormat.RGBA32ui => new ColorFormat(32, 32, 32, 32),
                DataFormat.RGBA32i => new ColorFormat(32, 32, 32, 32),
                DataFormat.RGBA32f => new ColorFormat(32, 32, 32, 32),


                DataFormat.RGB64ui => new ColorFormat(64, 64, 64, 0),
                DataFormat.RGB64i => new ColorFormat(64, 64, 64, 0),
                DataFormat.RGB64f => new ColorFormat(64, 64, 64, 0),

                DataFormat.RGBA64ui => new ColorFormat(64, 64, 64, 64),
                DataFormat.RGBA64i => new ColorFormat(64, 64, 64, 64),
                DataFormat.RGBA64f => new ColorFormat(64, 64, 64, 64),

                _ => ColorFormat.Empty,
            };
#endif

        public static DataFormat ToDataFormat(this PixelFormat format, PixelType type)
            => format switch
            {
                //TODO: SRGB?
                PixelFormat.Rgb => type switch
                {
                    PixelType.Byte => DataFormat.RGB8n,
                    PixelType.UnsignedByte => DataFormat.RGB8un,
                    PixelType.Short => DataFormat.RGB16n,
                    PixelType.UnsignedShort => DataFormat.RGB16un,
                    PixelType.Int => DataFormat.RGB32i,
                    PixelType.UnsignedInt => DataFormat.RGB32ui,
                    _ => DataFormat.Undefined,
                },
                PixelFormat.Rgba => type switch
                {
                    PixelType.Byte => DataFormat.RGBA8n,
                    PixelType.UnsignedByte => DataFormat.RGBA8un,
                    PixelType.Short => DataFormat.RGBA16n,
                    PixelType.UnsignedShort => DataFormat.RGBA16un,
                    PixelType.Int => DataFormat.RGBA32i,
                    PixelType.UnsignedInt => DataFormat.RGBA32ui,
                    _ => DataFormat.Undefined,
                },
                _ => DataFormat.Undefined,
            };


        public static int ToByteCount(this PixelType type)
            => type switch
            {
                PixelType.Byte => 1,
                PixelType.UnsignedByte => 1,
                PixelType.Short => 2,
                PixelType.UnsignedShort => 2,
                PixelType.Int => 4,
                PixelType.UnsignedInt => 4,
                PixelType.Float => 4,
                PixelType.HalfFloat => 2,
                PixelType.UnsignedInt248 => 4,
                PixelType.Float32UnsignedInt248Rev => 8,
                _ => 0,
            };
        public static int ToElementCount(this PixelFormat format)
            => format switch
            {
                PixelFormat.DepthComponent => 1,
                PixelFormat.Red => 1,
                PixelFormat.Alpha => 1,
                PixelFormat.Rgb => 3,
                PixelFormat.Rgba => 4,
                PixelFormat.Luminance => 4,
                PixelFormat.LuminanceAlpha => 4,
                PixelFormat.Rg => 2,
                PixelFormat.RgInteger => 2,
                PixelFormat.RgbInteger => 3,
                PixelFormat.RgbaInteger => 4,
                PixelFormat.DepthStencil => 1,
                _ => 0,
            };
        public static void ToPixelFormat(this DataFormat format, out int pixelInternalFormat, out PixelFormat pixelFormat, out PixelType pixelType)
        {
            switch (format)
            {
                case DataFormat.R8un: pixelInternalFormat = (int)GLPixelInternalFormat.R8Snorm; pixelFormat = PixelFormat.Red; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.R8n: pixelInternalFormat = (int)GLPixelInternalFormat.R8; pixelFormat = PixelFormat.Red; pixelType = PixelType.Byte; break;
                case DataFormat.R8us: pixelInternalFormat = (int)GLPixelInternalFormat.R8; pixelFormat = PixelFormat.Red; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.R8s: pixelInternalFormat = (int)GLPixelInternalFormat.R8; pixelFormat = PixelFormat.Red; pixelType = PixelType.Byte; break;
                case DataFormat.R8ui: pixelInternalFormat = (int)GLPixelInternalFormat.R8ui; pixelFormat = PixelFormat.Red; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.R8i: pixelInternalFormat = (int)GLPixelInternalFormat.R8i; pixelFormat = PixelFormat.Red; pixelType = PixelType.Byte; break;
                //case DataFormat.R8i: unsupported

                case DataFormat.RG8un: pixelInternalFormat = (int)GLPixelInternalFormat.Rg8Snorm; pixelFormat = PixelFormat.Rg; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.RG8n: pixelInternalFormat = (int)GLPixelInternalFormat.Rg8; pixelFormat = PixelFormat.Rg; pixelType = PixelType.Byte; break;
                case DataFormat.RG8us: pixelInternalFormat = (int)GLPixelInternalFormat.Rg8; pixelFormat = PixelFormat.Rg; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.RG8s: pixelInternalFormat = (int)GLPixelInternalFormat.Rg8; pixelFormat = PixelFormat.Rg; pixelType = PixelType.Byte; break;
                case DataFormat.RG8ui: pixelInternalFormat = (int)GLPixelInternalFormat.Rg8ui; pixelFormat = PixelFormat.Rg; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.RG8i: pixelInternalFormat = (int)GLPixelInternalFormat.Rg8i; pixelFormat = PixelFormat.Rg; pixelType = PixelType.Byte; break;
                //case DataFormat.RG8i: unsupported

                case DataFormat.RGB8un: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb8Snorm; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.RGB8n: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb8; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.Byte; break;
                case DataFormat.RGB8us: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb8; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.RGB8s: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb8; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.Byte; break;
                case DataFormat.RGB8ui: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb8ui; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.RGB8i: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb8i; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.Byte; break;
                case DataFormat.RGB8srgb: pixelInternalFormat = (int)GLPixelInternalFormat.Srgb8; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.UnsignedByte; break;

                //case DataFormat.BGR8un: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb8Snorm; pixelFormat = PixelFormat.Bgr; pixelType = PixelType.UnsignedByte; break;
                //case DataFormat.BGR8n: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb8; pixelFormat = PixelFormat.Bgr; pixelType = PixelType.Byte; break;
                //case DataFormat.BGR8us: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb8; pixelFormat = PixelFormat.Bgr; pixelType = PixelType.UnsignedByte; break;
                //case DataFormat.BGR8s: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb8; pixelFormat = PixelFormat.Bgr; pixelType = PixelType.Byte; break;
                //case DataFormat.BGR8ui: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb8ui; pixelFormat = PixelFormat.Bgr; pixelType = PixelType.UnsignedByte; break;
                //case DataFormat.BGR8i: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb8i; pixelFormat = PixelFormat.Bgr; pixelType = PixelType.Byte; break;

                case DataFormat.RGBA8un: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba8; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.RGBA8n: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba8; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.Byte; break;
                case DataFormat.RGBA8us: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba8; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.RGBA8s: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba8; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.Byte; break;
                case DataFormat.RGBA8ui: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba8ui; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.RGBA8i: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba8i; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.Byte; break;
                case DataFormat.RGBA8srgb: pixelInternalFormat = (int)GLPixelInternalFormat.Srgb8Alpha8; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.UnsignedByte; break;

                //case DataFormat.BGRA8un: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba8Snorm; pixelFormat = PixelFormat.Bgra; pixelType = PixelType.UnsignedByte; break;
                //case DataFormat.BGRA8n: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba8; pixelFormat = PixelFormat.Bgra; pixelType = PixelType.Byte; break;
                //case DataFormat.BGRA8us: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba8; pixelFormat = PixelFormat.Bgra; pixelType = PixelType.UnsignedByte; break;
                //case DataFormat.BGRA8s: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba8; pixelFormat = PixelFormat.Bgra; pixelType = PixelType.Byte; break;
                //case DataFormat.BGRA8ui: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba8ui; pixelFormat = PixelFormat.Bgra; pixelType = PixelType.UnsignedByte; break;
                //case DataFormat.BGRA8i: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba8i; pixelFormat = PixelFormat.Bgra; pixelType = PixelType.Byte; break;


                case DataFormat.R16un: pixelInternalFormat = (int)GLPixelInternalFormat.R16Snorm; pixelFormat = PixelFormat.Red; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.R16n: pixelInternalFormat = (int)GLPixelInternalFormat.R16; pixelFormat = PixelFormat.Red; pixelType = PixelType.Short; break;
                case DataFormat.R16us: pixelInternalFormat = (int)GLPixelInternalFormat.R16; pixelFormat = PixelFormat.Red; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.R16s: pixelInternalFormat = (int)GLPixelInternalFormat.R16; pixelFormat = PixelFormat.Red; pixelType = PixelType.Short; break;
                case DataFormat.R16ui: pixelInternalFormat = (int)GLPixelInternalFormat.R16ui; pixelFormat = PixelFormat.Red; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.R16i: pixelInternalFormat = (int)GLPixelInternalFormat.R16i; pixelFormat = PixelFormat.Red; pixelType = PixelType.Short; break;
                case DataFormat.R16f: pixelInternalFormat = (int)GLPixelInternalFormat.R16f; pixelFormat = PixelFormat.Red; pixelType = PixelType.HalfFloat; break;

                case DataFormat.RG16un: pixelInternalFormat = (int)GLPixelInternalFormat.Rg16Snorm; pixelFormat = PixelFormat.Rg; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.RG16n: pixelInternalFormat = (int)GLPixelInternalFormat.Rg16; pixelFormat = PixelFormat.Rg; pixelType = PixelType.Short; break;
                case DataFormat.RG16us: pixelInternalFormat = (int)GLPixelInternalFormat.Rg16; pixelFormat = PixelFormat.Rg; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.RG16s: pixelInternalFormat = (int)GLPixelInternalFormat.Rg16; pixelFormat = PixelFormat.Rg; pixelType = PixelType.Short; break;
                case DataFormat.RG16ui: pixelInternalFormat = (int)GLPixelInternalFormat.Rg16ui; pixelFormat = PixelFormat.Rg; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.RG16i: pixelInternalFormat = (int)GLPixelInternalFormat.Rg16i; pixelFormat = PixelFormat.Rg; pixelType = PixelType.Short; break;
                case DataFormat.RG16f: pixelInternalFormat = (int)GLPixelInternalFormat.Rg16f; pixelFormat = PixelFormat.Rg; pixelType = PixelType.HalfFloat; break;

                case DataFormat.RGB16un: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb16Snorm; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.RGB16n: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb16; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.Short; break;
                case DataFormat.RGB16us: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb16; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.RGB16s: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb16; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.Short; break;
                case DataFormat.RGB16ui: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb16ui; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.RGB16i: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb16i; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.Short; break;
                case DataFormat.RGB16f: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb16f; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.HalfFloat; break;

                case DataFormat.RGBA16un: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba16Snorm; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.RGBA16n: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba16; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.Short; break;
                case DataFormat.RGBA16us: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba16; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.RGBA16s: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba16; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.Short; break;
                case DataFormat.RGBA16ui: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba16ui; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.RGBA16i: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba16i; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.Short; break;
                case DataFormat.RGBA16f: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba16f; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.HalfFloat; break;


                case DataFormat.R32ui: pixelInternalFormat = (int)GLPixelInternalFormat.R32ui; pixelFormat = PixelFormat.Red; pixelType = PixelType.UnsignedInt; break;
                case DataFormat.R32i: pixelInternalFormat = (int)GLPixelInternalFormat.R32i; pixelFormat = PixelFormat.Red; pixelType = PixelType.Int; break;
                case DataFormat.R32f: pixelInternalFormat = (int)GLPixelInternalFormat.R32f; pixelFormat = PixelFormat.Red; pixelType = PixelType.Float; break;

                case DataFormat.RG32ui: pixelInternalFormat = (int)GLPixelInternalFormat.Rg32ui; pixelFormat = PixelFormat.Rg; pixelType = PixelType.UnsignedInt; break;
                case DataFormat.RG32i: pixelInternalFormat = (int)GLPixelInternalFormat.Rg32i; pixelFormat = PixelFormat.Rg; pixelType = PixelType.Int; break;
                case DataFormat.RG32f: pixelInternalFormat = (int)GLPixelInternalFormat.Rg32f; pixelFormat = PixelFormat.Rg; pixelType = PixelType.Float; break;

                case DataFormat.RGB32ui: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb32ui; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.UnsignedInt; break;
                case DataFormat.RGB32i: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb32i; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.Int; break;
                case DataFormat.RGB32f: pixelInternalFormat = (int)GLPixelInternalFormat.Rgb32f; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.Float; break;

                case DataFormat.RGBA32ui: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba32ui; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.UnsignedInt; break;
                case DataFormat.RGBA32i: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba32i; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.Int; break;
                case DataFormat.RGBA32f: pixelInternalFormat = (int)GLPixelInternalFormat.Rgba32f; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.Float; break;


                //64 bit formats from extensions?
                case DataFormat.R64ui: throw new NotSupportedException($"{format} is not supported in OpenGL!");
                case DataFormat.R64i: throw new NotSupportedException($"{format} is not supported in OpenGL!");
                case DataFormat.R64f: throw new NotSupportedException($"{format} is not supported in OpenGL!");

                case DataFormat.RG64ui: throw new NotSupportedException($"{format} is not supported in OpenGL!");
                case DataFormat.RG64i: throw new NotSupportedException($"{format} is not supported in OpenGL!");
                case DataFormat.RG64f: throw new NotSupportedException($"{format} is not supported in OpenGL!");

                case DataFormat.RGB64ui: throw new NotSupportedException($"{format} is not supported in OpenGL!");
                case DataFormat.RGB64i: throw new NotSupportedException($"{format} is not supported in OpenGL!");
                case DataFormat.RGB64f: throw new NotSupportedException($"{format} is not supported in OpenGL!");

                case DataFormat.RGBA64ui: throw new NotSupportedException($"{format} is not supported in OpenGL!");
                case DataFormat.RGBA64i: throw new NotSupportedException($"{format} is not supported in OpenGL!");
                case DataFormat.RGBA64f: throw new NotSupportedException($"{format} is not supported in OpenGL!");


                case DataFormat.Depth16un: pixelInternalFormat = (int)GLPixelInternalFormat.DepthComponent16; pixelFormat = PixelFormat.DepthComponent; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.Depth24un: pixelInternalFormat = (int)GLPixelInternalFormat.DepthComponent24; pixelFormat = PixelFormat.DepthComponent; pixelType = PixelType.UnsignedInt; break;
                case DataFormat.Depth32f: pixelInternalFormat = (int)GLPixelInternalFormat.DepthComponent32f; pixelFormat = PixelFormat.DepthComponent; pixelType = PixelType.Float; break;
                case DataFormat.Stencil8ui: pixelInternalFormat = (int)GLPixelInternalFormat.DepthStencil; pixelFormat = (PixelFormat)GLPixelFormat.StencilIndex; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.Depth16un_Stencil8ui: throw new NotSupportedException($"{format} is not supported in OpenGL!");
                case DataFormat.Depth24un_Stencil8ui: pixelInternalFormat = (int)GLPixelInternalFormat.Depth24Stencil8; pixelFormat = PixelFormat.DepthStencil; pixelType = PixelType.UnsignedInt248; break;
                case DataFormat.Depth32f_Stencil8ui: pixelInternalFormat = (int)GLPixelInternalFormat.Depth32fStencil8; pixelFormat = PixelFormat.DepthStencil; pixelType = PixelType.Float32UnsignedInt248Rev; break;

                case DataFormat.Undefined:
                default: pixelInternalFormat = (int)GLPixelInternalFormat.One; pixelFormat = PixelFormat.Red; pixelType = PixelType.Byte; break;
            }
        }

        public static RenderbufferInternalFormat ToRenderbufferInternalFormat(this DataFormat format)
            => format switch
            {
                //DataFormat.R8un => RenderbufferInternalFormat.R8Snorm,
                DataFormat.R8n => RenderbufferInternalFormat.R8,
                DataFormat.R8us => RenderbufferInternalFormat.R8,
                DataFormat.R8s => RenderbufferInternalFormat.R8,
                DataFormat.R8ui => RenderbufferInternalFormat.R8ui,
                DataFormat.R8i => RenderbufferInternalFormat.R8i,
                //DataFormat.R8srgb => unsupported

                //DataFormat.RG8un => RenderbufferInternalFormat.Rg8Snorm,
                DataFormat.RG8n => RenderbufferInternalFormat.Rg8,
                DataFormat.RG8us => RenderbufferInternalFormat.Rg8,
                DataFormat.RG8s => RenderbufferInternalFormat.Rg8,
                DataFormat.RG8ui => RenderbufferInternalFormat.Rg8ui,
                DataFormat.RG8i => RenderbufferInternalFormat.Rg8i,
                //DataFormat.RG8srgb => unsupported

                //DataFormat.RGB8un => RenderbufferInternalFormat.Rgb8Snorm,
                DataFormat.RGB8n => RenderbufferInternalFormat.Rgb8,
                DataFormat.RGB8us => RenderbufferInternalFormat.Rgb8,
                DataFormat.RGB8s => RenderbufferInternalFormat.Rgb8,
#if !ANDROID
                DataFormat.RGB8srgb => RenderbufferInternalFormat.Srgb8,
#endif

                //DataFormat.BGR8un => RenderbufferInternalFormat.Rgb8Snorm,
                //DataFormat.BGR8n => RenderbufferInternalFormat.Rgb8,
                //DataFormat.BGR8us => RenderbufferInternalFormat.Rgb8,
                //DataFormat.BGR8s => RenderbufferInternalFormat.Rgb8,

                //DataFormat.RGBA8un => RenderbufferInternalFormat.Rgba8Snorm,
                DataFormat.RGBA8n => RenderbufferInternalFormat.Rgba8,
                DataFormat.RGBA8us => RenderbufferInternalFormat.Rgba8,
                DataFormat.RGBA8s => RenderbufferInternalFormat.Rgba8,
                DataFormat.RGBA8ui => RenderbufferInternalFormat.Rgba8ui,
                DataFormat.RGBA8i => RenderbufferInternalFormat.Rgba8i,
#if !ANDROID
                DataFormat.RGBA8srgb => RenderbufferInternalFormat.Srgb8Alpha8,
#endif

                //DataFormat.BGRA8un => RenderbufferInternalFormat.Rgba8Snorm,
                //DataFormat.BGRA8n => RenderbufferInternalFormat.Rgba8,
                //DataFormat.BGRA8us => RenderbufferInternalFormat.Rgba8,
                //DataFormat.BGRA8s => RenderbufferInternalFormat.Rgba8,
                //DataFormat.BGRA8ui => RenderbufferInternalFormat.Rgba8ui,
                //DataFormat.BGRA8i => RenderbufferInternalFormat.Rgba8i,


                //DataFormat.R16un => RenderbufferInternalFormat.R16Snorm,
                //DataFormat.R16n => RenderbufferInternalFormat.R16,
                //DataFormat.R16us => RenderbufferInternalFormat.R16,
                //DataFormat.R16s => RenderbufferInternalFormat.R16,
                DataFormat.R16ui => RenderbufferInternalFormat.R16ui,
                DataFormat.R16i => RenderbufferInternalFormat.R16i,

                //DataFormat.RG16un => RenderbufferInternalFormat.Rg16Snorm,
                //DataFormat.RG16n => RenderbufferInternalFormat.Rg16,
                //DataFormat.RG16us => RenderbufferInternalFormat.Rg16,
                //DataFormat.RG16s => RenderbufferInternalFormat.Rg16,
                DataFormat.RG16ui => RenderbufferInternalFormat.Rg16ui,
                DataFormat.RG16i => RenderbufferInternalFormat.Rg16i,

                //DataFormat.RGB16un => RenderbufferInternalFormat.Rgb16Snorm,
                //DataFormat.RGB16n => RenderbufferInternalFormat.Rgb16,
                //DataFormat.RGB16us => RenderbufferInternalFormat.Rgb16,
                //DataFormat.RGB16s => RenderbufferInternalFormat.Rgb16,

                //DataFormat.RGBA16un => RenderbufferInternalFormat.Rgba16Snorm,
                //DataFormat.RGBA16n => RenderbufferInternalFormat.Rgba16,
                //DataFormat.RGBA16us => RenderbufferInternalFormat.Rgba16,
                //DataFormat.RGBA16s => RenderbufferInternalFormat.Rgba16,
                DataFormat.RGBA16ui => RenderbufferInternalFormat.Rgba16ui,
                DataFormat.RGBA16i => RenderbufferInternalFormat.Rgba16i,


                DataFormat.R32ui => RenderbufferInternalFormat.R32ui,
                DataFormat.R32i => RenderbufferInternalFormat.R32i,

                DataFormat.RG32ui => RenderbufferInternalFormat.Rg32ui,
                DataFormat.RG32i => RenderbufferInternalFormat.Rg32i,

                DataFormat.RGBA32ui => RenderbufferInternalFormat.Rgba32ui,
                DataFormat.RGBA32i => RenderbufferInternalFormat.Rgba32i,


                //64 bit formats from extensions?
                DataFormat.R64ui => throw new NotSupportedException($"{format} is not supported in OpenGL!"),
                DataFormat.R64i => throw new NotSupportedException($"{format} is not supported in OpenGL!"),
                DataFormat.R64f => throw new NotSupportedException($"{format} is not supported in OpenGL!"),

                DataFormat.RG64ui => throw new NotSupportedException($"{format} is not supported in OpenGL!"),
                DataFormat.RG64i => throw new NotSupportedException($"{format} is not supported in OpenGL!"),
                DataFormat.RG64f => throw new NotSupportedException($"{format} is not supported in OpenGL!"),

                DataFormat.RGB64ui => throw new NotSupportedException($"{format} is not supported in OpenGL!"),
                DataFormat.RGB64i => throw new NotSupportedException($"{format} is not supported in OpenGL!"),
                DataFormat.RGB64f => throw new NotSupportedException($"{format} is not supported in OpenGL!"),

                DataFormat.RGBA64ui => throw new NotSupportedException($"{format} is not supported in OpenGL!"),
                DataFormat.RGBA64i => throw new NotSupportedException($"{format} is not supported in OpenGL!"),
                DataFormat.RGBA64f => throw new NotSupportedException($"{format} is not supported in OpenGL!"),


                DataFormat.Depth16un => RenderbufferInternalFormat.DepthComponent16,
                DataFormat.Depth24un => RenderbufferInternalFormat.DepthComponent24,
                DataFormat.Stencil8ui => RenderbufferInternalFormat.StencilIndex8,
                DataFormat.Depth16un_Stencil8ui => throw new NotSupportedException($"{format} is not supported in OpenGL!"),
                DataFormat.Depth24un_Stencil8ui => RenderbufferInternalFormat.Depth24Stencil8,

#if ANDROID
                DataFormat.RGB8ui => RenderbufferInternalFormat.Rgb8,
                DataFormat.RGB8i => RenderbufferInternalFormat.Rgb8,

                DataFormat.BGR8ui => RenderbufferInternalFormat.Rgb8,
                DataFormat.BGR8i => RenderbufferInternalFormat.Rgb8,                

                DataFormat.Depth32f => RenderbufferInternalFormat.DepthComponent32F,
                DataFormat.Depth32f_Stencil8ui => RenderbufferInternalFormat.Depth32FStencil8,
#else
                DataFormat.RGB8ui => RenderbufferInternalFormat.Rgb8ui,
                DataFormat.RGB8i => RenderbufferInternalFormat.Rgb8i,

                DataFormat.BGR8ui => RenderbufferInternalFormat.Rgb8ui,
                DataFormat.BGR8i => RenderbufferInternalFormat.Rgb8i,                

                DataFormat.R16f => RenderbufferInternalFormat.R16f,

                DataFormat.RG16f => RenderbufferInternalFormat.Rg16f,

                DataFormat.RGB16ui => RenderbufferInternalFormat.Rgb16ui,
                DataFormat.RGB16i => RenderbufferInternalFormat.Rgb16i,
                DataFormat.RGB16f => RenderbufferInternalFormat.Rgb16f,                

                DataFormat.RGBA16f => RenderbufferInternalFormat.Rgba16f,

                DataFormat.R32f => RenderbufferInternalFormat.R32f,

                DataFormat.RG32f => RenderbufferInternalFormat.Rg32f,

                DataFormat.RGB32ui => RenderbufferInternalFormat.Rgb32ui,
                DataFormat.RGB32i => RenderbufferInternalFormat.Rgb32i,
                DataFormat.RGB32f => RenderbufferInternalFormat.Rgb32f,       
                
                DataFormat.RGBA32f => RenderbufferInternalFormat.Rgba32f,

                DataFormat.Depth32f => RenderbufferInternalFormat.DepthComponent32f,
                DataFormat.Depth32f_Stencil8ui => RenderbufferInternalFormat.Depth32fStencil8,
#endif

                DataFormat.Undefined => 0,
                _ => 0,
            };


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRenderBuffer(this DataFormat format, TextureType textureType, in MemoryType memoryType, uint mipLevels)
            => textureType.IsRenderBuffer(memoryType, mipLevels) && format.ToRenderbufferInternalFormat() != 0;


        public static ClearBufferMask ToClearBufferMask(this IGLFrameBuffer.BlitMask blitMask)
        {
            ClearBufferMask result = 0;

            if (blitMask.HasFlag(IGLFrameBuffer.BlitMask.Color)) result |= ClearBufferMask.ColorBufferBit;
            if (blitMask.HasFlag(IGLFrameBuffer.BlitMask.Depth)) result |= ClearBufferMask.DepthBufferBit;
            if (blitMask.HasFlag(IGLFrameBuffer.BlitMask.Stencil)) result |= ClearBufferMask.StencilBufferBit;

            return result;
        }

        public static BlendingFactorSrc ToBlendingFactorSrc(this BlendFactor factor)
            => factor switch
            {
                BlendFactor.Zero => BlendingFactorSrc.Zero,
                BlendFactor.One => BlendingFactorSrc.One,
                BlendFactor.SourceColor => BlendingFactorSrc.SrcColor,
                BlendFactor.OneMinusSourceColor => BlendingFactorSrc.OneMinusSrcColor,
                BlendFactor.DestinationColor => BlendingFactorSrc.DstColor,
                BlendFactor.OneMinusDestinationColor => BlendingFactorSrc.OneMinusDstColor,
                BlendFactor.SourceAlpha => BlendingFactorSrc.SrcAlpha,
                BlendFactor.OneMinusSourceAlpha => BlendingFactorSrc.OneMinusSrcAlpha,
                BlendFactor.DestinationAlpha => BlendingFactorSrc.DstAlpha,
                BlendFactor.OneMinusDestinationAlpha => BlendingFactorSrc.OneMinusDstAlpha,
                BlendFactor.ConstantColor => BlendingFactorSrc.ConstantColor,
                BlendFactor.OneMinusConstantColor => BlendingFactorSrc.OneMinusConstantColor,
                BlendFactor.ConstantAlpha => BlendingFactorSrc.ConstantAlpha,
                BlendFactor.OneMinusConstantAlpha => BlendingFactorSrc.OneMinusConstantAlpha,
                BlendFactor.SourceAlphaSaturate => BlendingFactorSrc.SrcAlphaSaturate,
                BlendFactor.DualSourceColor => BlendingFactorSrc.SrcColor, //Dual Source Blend not supported
                BlendFactor.OneMinusDualSourceColor => BlendingFactorSrc.OneMinusSrcColor, //Dual Source Blend not supported
                BlendFactor.DualSourceAlpha => BlendingFactorSrc.SrcAlpha, //Dual Source Blend not supported
                BlendFactor.OneMinusDualSourceAlpha => BlendingFactorSrc.OneMinusSrcAlpha, //Dual Source Blend not supported
                _ => BlendingFactorSrc.Zero,
            };
        public static BlendingFactorDest ToBlendingFactorDest(this BlendFactor factor)
            => factor switch
            {
                BlendFactor.Zero => BlendingFactorDest.Zero,
                BlendFactor.One => BlendingFactorDest.One,
                BlendFactor.SourceColor => BlendingFactorDest.SrcColor,
                BlendFactor.OneMinusSourceColor => BlendingFactorDest.OneMinusSrcColor,
                BlendFactor.DestinationColor => BlendingFactorDest.DstColor,
                BlendFactor.OneMinusDestinationColor => BlendingFactorDest.OneMinusDstColor,
                BlendFactor.SourceAlpha => BlendingFactorDest.SrcAlpha,
                BlendFactor.OneMinusSourceAlpha => BlendingFactorDest.OneMinusSrcAlpha,
                BlendFactor.DestinationAlpha => BlendingFactorDest.DstAlpha,
                BlendFactor.OneMinusDestinationAlpha => BlendingFactorDest.OneMinusDstAlpha,
                BlendFactor.ConstantColor => BlendingFactorDest.ConstantColor,
                BlendFactor.OneMinusConstantColor => BlendingFactorDest.OneMinusConstantColor,
                BlendFactor.ConstantAlpha => BlendingFactorDest.ConstantAlpha,
                BlendFactor.OneMinusConstantAlpha => BlendingFactorDest.OneMinusConstantAlpha,
                BlendFactor.SourceAlphaSaturate => BlendingFactorDest.SrcAlphaSaturate,
                BlendFactor.DualSourceColor => BlendingFactorDest.SrcColor, //Dual Source Blend not supported
                BlendFactor.OneMinusDualSourceColor => BlendingFactorDest.OneMinusSrcColor, //Dual Source Blend not supported
                BlendFactor.DualSourceAlpha => BlendingFactorDest.SrcAlpha, //Dual Source Blend not supported
                BlendFactor.OneMinusDualSourceAlpha => BlendingFactorDest.OneMinusSrcAlpha, //Dual Source Blend not supported
                _ => BlendingFactorDest.Zero,
            };
        public static BlendEquationMode ToBlendEquationMode(this BlendOperation operation)
            => operation switch
            {
                BlendOperation.Add => BlendEquationMode.FuncAdd,
                BlendOperation.Subtract => BlendEquationMode.FuncSubtract,
                BlendOperation.ReverseSubtract => BlendEquationMode.FuncReverseSubtract,
                BlendOperation.Min => BlendEquationMode.Min,
                BlendOperation.Max => BlendEquationMode.Max,
                _ => BlendEquationMode.FuncAdd,
            };

        public static CullFaceMode ToCullFaceModeFlags(this CullMode cullMode)
            => cullMode switch
            {
                CullMode.Front => CullFaceMode.Front,
                CullMode.Back => CullFaceMode.Back,
                CullMode.Front | CullMode.Back => CullFaceMode.FrontAndBack,
                _ => CullFaceMode.Back,
            };
        public static FrontFaceDirection ToFrontFaceDirection(this WindingOrder windingOrder)
            => windingOrder switch
            {
                WindingOrder.CounterClockwise => FrontFaceDirection.Ccw,
                WindingOrder.Clockwise => FrontFaceDirection.Cw,
                _ => FrontFaceDirection.Ccw,
            };

        public static FramebufferAttachment ToFramebufferAttachment(this AttachmentType attachmentType, int attachmentTypeIndex)
            => attachmentType switch
            {
                AttachmentType.Color => FramebufferAttachment.ColorAttachment0 + attachmentTypeIndex,
                AttachmentType.Depth => FramebufferAttachment.DepthAttachment,
                AttachmentType.Stencil => FramebufferAttachment.StencilAttachment,
                AttachmentType.Depth | AttachmentType.Stencil => FramebufferAttachment.DepthStencilAttachment,
                _ => 0,
            };

        public static TextureMinFilter ToTextureMinFilter(this TextureFilterType filter, TextureMipMapType mipMap)
            => filter switch
            {
                TextureFilterType.Nearest => mipMap switch
                {
                    TextureMipMapType.NotUsed => TextureMinFilter.Nearest,
                    TextureMipMapType.Nearest => TextureMinFilter.NearestMipmapNearest,
                    TextureMipMapType.Linear => TextureMinFilter.NearestMipmapLinear,
                    _ => TextureMinFilter.Nearest,
                },
                TextureFilterType.Linear => mipMap switch
                {
                    TextureMipMapType.NotUsed => TextureMinFilter.Linear,
                    TextureMipMapType.Nearest => TextureMinFilter.LinearMipmapNearest,
                    TextureMipMapType.Linear => TextureMinFilter.LinearMipmapLinear,
                    _ => TextureMinFilter.Linear,
                },
                _ => TextureMinFilter.Nearest,
            };
        public static TextureMagFilter ToTextureMagFilter(this TextureFilterType filter)
            => filter switch
            {
                TextureFilterType.Nearest => TextureMagFilter.Nearest,
                TextureFilterType.Linear => TextureMagFilter.Linear,
                _ => TextureMagFilter.Nearest,
            };
        public static TextureWrapMode ToTextureWrapMode(this TextureWrapType wrap)
            => wrap switch
            {
                TextureWrapType.Repeat => TextureWrapMode.Repeat,
#if ANDROID
                TextureWrapType.MirrorRepeat => TextureWrapMode.MirroredRepeat,
#else
                TextureWrapType.ClampToBorder => TextureWrapMode.ClampToBorder,
#endif
                TextureWrapType.ClampToEdge => TextureWrapMode.ClampToEdge,
                TextureWrapType.MirrorClampToEdge => TextureWrapMode.ClampToEdge, //GL 4.4 and above...
                _ => TextureWrapMode.Repeat,
            };



        [Conditional("DEBUG")]
        public static void DebugWriteGLErrors(string location)
        {
            ErrorCode error;
            while (true)
            {
#if ANDROID
                error = GL.GetErrorCode();
#else
                error = GL.GetError();
#endif
                if (error != ErrorCode.NoError)
                    Debug.WriteLine($"OpenGL ES 3.0 ERROR - {location} - {error}");
                else break;
            }
        }
        [Conditional("DEBUG")]
        public static void DebugWriteGLErrors(string location, OpenGL.Commands.IGLCommand command)
        {
            ErrorCode error;
            while (true)
            {
#if ANDROID
                error = GL.GetErrorCode();
#else
                error = GL.GetError();
#endif
                if (error != ErrorCode.NoError)
                    Debug.WriteLine($"OpenGL ES 3.0 ERROR - {location} - {command} - {error}");
                else break;
            }
        }

    }
}
