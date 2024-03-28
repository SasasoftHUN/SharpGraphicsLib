using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
#if OPENTK4
using OpenTK.Mathematics;
#endif
using SharpGraphics.Shaders;

namespace SharpGraphics.OpenGL.OpenGLCore
{
    internal static class GLCoreUtils
    {

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
                //TODO: Implement for Geometry Shader
                //GraphicsShaderStages.Geometry => ShaderType.GeometryShader,
                //TODO: Implement for Tessellation Shader
                //GraphicsShaderStages.TessellationControl => ShaderType.TessControlShader,
                //GraphicsShaderStages.TessellationEvaluation => ShaderType.TessEvaluationShader,
                GraphicsShaderStages.Fragment => ShaderType.FragmentShader,
                _ => ShaderType.VertexShader,
            };

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DrawElementsType ToDrawElementsType(this GraphicsCommandBuffer.IndexType indexType)
            => indexType switch
            {
                GraphicsCommandBuffer.IndexType.UnsignedShort => DrawElementsType.UnsignedShort,
                GraphicsCommandBuffer.IndexType.UnsignedInt => DrawElementsType.UnsignedInt,
                _ => DrawElementsType.UnsignedShort,
            };

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BufferStorageFlags ToBufferStorageFlags(this DataBufferType type, MappableMemoryType? memoryType)
            => memoryType.HasValue ? type.ToBufferStorageFlags(memoryType.Value) : type.ToBufferStorageFlags();
        public static BufferStorageFlags ToBufferStorageFlags(this DataBufferType type) //Device Only
        {
            //Cannot be sure, that StoreData will use StagingBuffer so it needs DynamicStorageBit
            //Still, it can be used optimally with CopyDestination flag only
            if (type.HasFlag(DataBufferType.Store)) return BufferStorageFlags.DynamicStorageBit;

            return BufferStorageFlags.None;
        }
        public static BufferStorageFlags ToBufferStorageFlags(this DataBufferType type, MappableMemoryType memoryType) //Mappable
        {
            BufferStorageFlags mappableFlags = BufferStorageFlags.MapWriteBit | BufferStorageFlags.MapReadBit | BufferStorageFlags.MapPersistentBit;

            if (type.HasFlag(DataBufferType.Store))
                mappableFlags |= BufferStorageFlags.DynamicStorageBit;

            if (memoryType.HasFlag(MappableMemoryType.Coherent) || memoryType.HasFlag(MappableMemoryType.Cached))
                mappableFlags |= BufferStorageFlags.MapCoherentBit;

            return memoryType.HasFlag(MappableMemoryType.DeviceLocal) ? mappableFlags : (mappableFlags | BufferStorageFlags.ClientStorageBit);
        }

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
                PixelFormat.Bgr => type switch
                {
                    PixelType.Byte => DataFormat.BGR8n,
                    PixelType.UnsignedByte => DataFormat.BGR8un,
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
                PixelFormat.Bgra => type switch
                {
                    PixelType.Byte => DataFormat.BGRA8n,
                    PixelType.UnsignedByte => DataFormat.BGRA8un,
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
                PixelFormat.UnsignedShort => 1,
                PixelFormat.UnsignedInt => 1,
                PixelFormat.ColorIndex => 1,
                PixelFormat.StencilIndex => 1,
                PixelFormat.DepthComponent => 1,
                PixelFormat.Red => 1,
                PixelFormat.Green => 1,
                PixelFormat.Blue => 1,
                PixelFormat.Alpha => 1,
                PixelFormat.Rgb => 3,
                PixelFormat.Rgba => 4,
                PixelFormat.Luminance => 4,
                PixelFormat.LuminanceAlpha => 4,
                PixelFormat.Bgr => 3,
                PixelFormat.Bgra => 4,
                PixelFormat.Rg => 2,
                PixelFormat.RgInteger => 2,
                PixelFormat.DepthStencil => 1,
                PixelFormat.RgbInteger => 3,
                PixelFormat.RgbaInteger => 4,
                PixelFormat.BgrInteger => 3,
                PixelFormat.BgraInteger => 4,
                _ => 0,
            };
        public static void ToPixelFormat(this DataFormat format, out PixelInternalFormat pixelInternalFormat, out PixelFormat pixelFormat, out PixelType pixelType)
        {
            switch (format)
            {
                case DataFormat.R8un: pixelInternalFormat = PixelInternalFormat.R8Snorm; pixelFormat = PixelFormat.Red; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.R8n: pixelInternalFormat = PixelInternalFormat.R8; pixelFormat = PixelFormat.Red; pixelType = PixelType.Byte; break;
                case DataFormat.R8us: pixelInternalFormat = PixelInternalFormat.R8; pixelFormat = PixelFormat.Red; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.R8s: pixelInternalFormat = PixelInternalFormat.R8; pixelFormat = PixelFormat.Red; pixelType = PixelType.Byte; break;
                case DataFormat.R8ui: pixelInternalFormat = PixelInternalFormat.R8ui; pixelFormat = PixelFormat.Red; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.R8i: pixelInternalFormat = PixelInternalFormat.R8i; pixelFormat = PixelFormat.Red; pixelType = PixelType.Byte; break;
                //case DataFormat.R8srgb: unsupported

                case DataFormat.RG8un: pixelInternalFormat = PixelInternalFormat.Rg8Snorm; pixelFormat = PixelFormat.Rg; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.RG8n: pixelInternalFormat = PixelInternalFormat.Rg8; pixelFormat = PixelFormat.Rg; pixelType = PixelType.Byte; break;
                case DataFormat.RG8us: pixelInternalFormat = PixelInternalFormat.Rg8; pixelFormat = PixelFormat.Rg; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.RG8s: pixelInternalFormat = PixelInternalFormat.Rg8; pixelFormat = PixelFormat.Rg; pixelType = PixelType.Byte; break;
                case DataFormat.RG8ui: pixelInternalFormat = PixelInternalFormat.Rg8ui; pixelFormat = PixelFormat.Rg; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.RG8i: pixelInternalFormat = PixelInternalFormat.Rg8i; pixelFormat = PixelFormat.Rg; pixelType = PixelType.Byte; break;
                //case DataFormat.RG8srgb: unsupported

                case DataFormat.RGB8un: pixelInternalFormat = PixelInternalFormat.Rgb8Snorm; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.RGB8n: pixelInternalFormat = PixelInternalFormat.Rgb8; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.Byte; break;
                case DataFormat.RGB8us: pixelInternalFormat = PixelInternalFormat.Rgb8; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.RGB8s: pixelInternalFormat = PixelInternalFormat.Rgb8; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.Byte; break;
                case DataFormat.RGB8ui: pixelInternalFormat = PixelInternalFormat.Rgb8ui; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.RGB8i: pixelInternalFormat = PixelInternalFormat.Rgb8i; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.Byte; break;
                case DataFormat.RGB8srgb: pixelInternalFormat = PixelInternalFormat.Srgb8; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.UnsignedByte; break;

                case DataFormat.BGR8un: pixelInternalFormat = PixelInternalFormat.Rgb8Snorm; pixelFormat = PixelFormat.Bgr; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.BGR8n: pixelInternalFormat = PixelInternalFormat.Rgb8; pixelFormat = PixelFormat.Bgr; pixelType = PixelType.Byte; break;
                case DataFormat.BGR8us: pixelInternalFormat = PixelInternalFormat.Rgb8; pixelFormat = PixelFormat.Bgr; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.BGR8s: pixelInternalFormat = PixelInternalFormat.Rgb8; pixelFormat = PixelFormat.Bgr; pixelType = PixelType.Byte; break;
                case DataFormat.BGR8ui: pixelInternalFormat = PixelInternalFormat.Rgb8ui; pixelFormat = PixelFormat.Bgr; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.BGR8i: pixelInternalFormat = PixelInternalFormat.Rgb8i; pixelFormat = PixelFormat.Bgr; pixelType = PixelType.Byte; break;
                case DataFormat.BGR8srgb: pixelInternalFormat = PixelInternalFormat.Srgb8; pixelFormat = PixelFormat.Bgr; pixelType = PixelType.Byte; break;

                case DataFormat.RGBA8un: pixelInternalFormat = PixelInternalFormat.Rgba8Snorm; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.RGBA8n: pixelInternalFormat = PixelInternalFormat.Rgba8; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.Byte; break;
                case DataFormat.RGBA8us: pixelInternalFormat = PixelInternalFormat.Rgba8; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.RGBA8s: pixelInternalFormat = PixelInternalFormat.Rgba8; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.Byte; break;
                case DataFormat.RGBA8ui: pixelInternalFormat = PixelInternalFormat.Rgba8ui; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.RGBA8i: pixelInternalFormat = PixelInternalFormat.Rgba8i; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.Byte; break;
                case DataFormat.RGBA8srgb: pixelInternalFormat = PixelInternalFormat.Srgb8Alpha8; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.UnsignedByte; break;

                case DataFormat.BGRA8un: pixelInternalFormat = PixelInternalFormat.Rgba8Snorm; pixelFormat = PixelFormat.Bgra; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.BGRA8n: pixelInternalFormat = PixelInternalFormat.Rgba8; pixelFormat = PixelFormat.Bgra; pixelType = PixelType.Byte; break;
                case DataFormat.BGRA8us: pixelInternalFormat = PixelInternalFormat.Rgba8; pixelFormat = PixelFormat.Bgra; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.BGRA8s: pixelInternalFormat = PixelInternalFormat.Rgba8; pixelFormat = PixelFormat.Bgra; pixelType = PixelType.Byte; break;
                case DataFormat.BGRA8ui: pixelInternalFormat = PixelInternalFormat.Rgba8ui; pixelFormat = PixelFormat.Bgra; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.BGRA8i: pixelInternalFormat = PixelInternalFormat.Rgba8i; pixelFormat = PixelFormat.Bgra; pixelType = PixelType.Byte; break;
                case DataFormat.BGRA8srgb: pixelInternalFormat = PixelInternalFormat.Srgb8Alpha8; pixelFormat = PixelFormat.Bgra; pixelType = PixelType.UnsignedByte; break;


                case DataFormat.R16un: pixelInternalFormat = PixelInternalFormat.R16Snorm; pixelFormat = PixelFormat.Red; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.R16n: pixelInternalFormat = PixelInternalFormat.R16; pixelFormat = PixelFormat.Red; pixelType = PixelType.Short; break;
                case DataFormat.R16us: pixelInternalFormat = PixelInternalFormat.R16; pixelFormat = PixelFormat.Red; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.R16s: pixelInternalFormat = PixelInternalFormat.R16; pixelFormat = PixelFormat.Red; pixelType = PixelType.Short; break;
                case DataFormat.R16ui: pixelInternalFormat = PixelInternalFormat.R16ui; pixelFormat = PixelFormat.Red; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.R16i: pixelInternalFormat = PixelInternalFormat.R16i; pixelFormat = PixelFormat.Red; pixelType = PixelType.Short; break;
                case DataFormat.R16f: pixelInternalFormat = PixelInternalFormat.R16f; pixelFormat = PixelFormat.Red; pixelType = PixelType.HalfFloat; break;

                case DataFormat.RG16un: pixelInternalFormat = PixelInternalFormat.Rg16Snorm; pixelFormat = PixelFormat.Rg; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.RG16n: pixelInternalFormat = PixelInternalFormat.Rg16; pixelFormat = PixelFormat.Rg; pixelType = PixelType.Short; break;
                case DataFormat.RG16us: pixelInternalFormat = PixelInternalFormat.Rg16; pixelFormat = PixelFormat.Rg; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.RG16s: pixelInternalFormat = PixelInternalFormat.Rg16; pixelFormat = PixelFormat.Rg; pixelType = PixelType.Short; break;
                case DataFormat.RG16ui: pixelInternalFormat = PixelInternalFormat.Rg16ui; pixelFormat = PixelFormat.Rg; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.RG16i: pixelInternalFormat = PixelInternalFormat.Rg16i; pixelFormat = PixelFormat.Rg; pixelType = PixelType.Short; break;
                case DataFormat.RG16f: pixelInternalFormat = PixelInternalFormat.Rg16f; pixelFormat = PixelFormat.Rg; pixelType = PixelType.HalfFloat; break;

                case DataFormat.RGB16un: pixelInternalFormat = PixelInternalFormat.Rgb16Snorm; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.RGB16n: pixelInternalFormat = PixelInternalFormat.Rgb16; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.Short; break;
                case DataFormat.RGB16us: pixelInternalFormat = PixelInternalFormat.Rgb16; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.RGB16s: pixelInternalFormat = PixelInternalFormat.Rgb16; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.Short; break;
                case DataFormat.RGB16ui: pixelInternalFormat = PixelInternalFormat.Rgb16ui; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.RGB16i: pixelInternalFormat = PixelInternalFormat.Rgb16i; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.Short; break;
                case DataFormat.RGB16f: pixelInternalFormat = PixelInternalFormat.Rgb16f; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.HalfFloat; break;

                case DataFormat.RGBA16un: pixelInternalFormat = PixelInternalFormat.Rgba16Snorm; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.RGBA16n: pixelInternalFormat = PixelInternalFormat.Rgba16; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.Short; break;
                case DataFormat.RGBA16us: pixelInternalFormat = PixelInternalFormat.Rgba16; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.RGBA16s: pixelInternalFormat = PixelInternalFormat.Rgba16; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.Short; break;
                case DataFormat.RGBA16ui: pixelInternalFormat = PixelInternalFormat.Rgba16ui; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.RGBA16i: pixelInternalFormat = PixelInternalFormat.Rgba16i; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.Short; break;
                case DataFormat.RGBA16f: pixelInternalFormat = PixelInternalFormat.Rgba16f; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.HalfFloat; break;


                case DataFormat.R32ui: pixelInternalFormat = PixelInternalFormat.R32ui; pixelFormat = PixelFormat.Red; pixelType = PixelType.UnsignedInt; break;
                case DataFormat.R32i: pixelInternalFormat = PixelInternalFormat.R32i; pixelFormat = PixelFormat.Red; pixelType = PixelType.Int; break;
                case DataFormat.R32f: pixelInternalFormat = PixelInternalFormat.R32f; pixelFormat = PixelFormat.Red; pixelType = PixelType.Float; break;

                case DataFormat.RG32ui: pixelInternalFormat = PixelInternalFormat.Rg32ui; pixelFormat = PixelFormat.Rg; pixelType = PixelType.UnsignedInt; break;
                case DataFormat.RG32i: pixelInternalFormat = PixelInternalFormat.Rg32i; pixelFormat = PixelFormat.Rg; pixelType = PixelType.Int; break;
                case DataFormat.RG32f: pixelInternalFormat = PixelInternalFormat.Rg32f; pixelFormat = PixelFormat.Rg; pixelType = PixelType.Float; break;

                case DataFormat.RGB32ui: pixelInternalFormat = PixelInternalFormat.Rgb32ui; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.UnsignedInt; break;
                case DataFormat.RGB32i: pixelInternalFormat = PixelInternalFormat.Rgb32i; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.Int; break;
                case DataFormat.RGB32f: pixelInternalFormat = PixelInternalFormat.Rgb32f; pixelFormat = PixelFormat.Rgb; pixelType = PixelType.Float; break;

                case DataFormat.RGBA32ui: pixelInternalFormat = PixelInternalFormat.Rgba32ui; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.UnsignedInt; break;
                case DataFormat.RGBA32i: pixelInternalFormat = PixelInternalFormat.Rgba32i; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.Int; break;
                case DataFormat.RGBA32f: pixelInternalFormat = PixelInternalFormat.Rgba32f; pixelFormat = PixelFormat.Rgba; pixelType = PixelType.Float; break;


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


                case DataFormat.Depth16un: pixelInternalFormat = PixelInternalFormat.DepthComponent16; pixelFormat = PixelFormat.DepthComponent; pixelType = PixelType.UnsignedShort; break;
                case DataFormat.Depth24un: pixelInternalFormat = PixelInternalFormat.DepthComponent24; pixelFormat = PixelFormat.DepthComponent; pixelType = PixelType.UnsignedInt; break;
                case DataFormat.Depth32f: pixelInternalFormat = PixelInternalFormat.DepthComponent32f; pixelFormat = PixelFormat.DepthComponent; pixelType = PixelType.Float; break;
                case DataFormat.Stencil8ui: pixelInternalFormat = PixelInternalFormat.DepthStencil; pixelFormat = PixelFormat.StencilIndex; pixelType = PixelType.UnsignedByte; break;
                case DataFormat.Depth16un_Stencil8ui: throw new NotSupportedException($"{format} is not supported in OpenGL!");
                case DataFormat.Depth24un_Stencil8ui: pixelInternalFormat = PixelInternalFormat.Depth24Stencil8; pixelFormat = PixelFormat.DepthStencil; pixelType = PixelType.UnsignedInt248; break;
                case DataFormat.Depth32f_Stencil8ui: pixelInternalFormat = PixelInternalFormat.Depth32fStencil8; pixelFormat = PixelFormat.DepthStencil; pixelType = PixelType.Float32UnsignedInt248Rev; break;

                case DataFormat.Undefined:
                default: pixelInternalFormat = PixelInternalFormat.One; pixelFormat = PixelFormat.Red; pixelType = PixelType.Byte; break;
            }
        }

        public static RenderbufferStorage ToRenderbufferStorage(this DataFormat format)
            => format switch
            {
                //DataFormat.R8un => RenderbufferStorage.R8Snorm,
                DataFormat.R8n => RenderbufferStorage.R8,
                DataFormat.R8us => RenderbufferStorage.R8,
                DataFormat.R8s => RenderbufferStorage.R8,
                DataFormat.R8ui => RenderbufferStorage.R8ui,
                DataFormat.R8i => RenderbufferStorage.R8i,
                //DataFormat.R8srgb => unsupported

                //DataFormat.RG8un => RenderbufferStorage.Rg8Snorm,
                DataFormat.RG8n => RenderbufferStorage.Rg8,
                DataFormat.RG8us => RenderbufferStorage.Rg8,
                DataFormat.RG8s => RenderbufferStorage.Rg8,
                DataFormat.RG8ui => RenderbufferStorage.Rg8ui,
                DataFormat.RG8i => RenderbufferStorage.Rg8i,
                //DataFormat.RG8srgb => unsupported

                //DataFormat.RGB8un => RenderbufferStorage.Rgb8Snorm,
                DataFormat.RGB8n => RenderbufferStorage.Rgb8,
                DataFormat.RGB8us => RenderbufferStorage.Rgb8,
                DataFormat.RGB8s => RenderbufferStorage.Rgb8,
                DataFormat.RGB8ui => RenderbufferStorage.Rgb8ui,
                DataFormat.RGB8i => RenderbufferStorage.Rgb8i,
                DataFormat.RGB8srgb => RenderbufferStorage.Srgb8,

                //DataFormat.BGR8un => RenderbufferStorage.Rgb8Snorm,
                //DataFormat.BGR8n => RenderbufferStorage.Rgb8,
                //DataFormat.BGR8us => RenderbufferStorage.Rgb8,
                //DataFormat.BGR8s => RenderbufferStorage.Rgb8,
                //DataFormat.BGR8ui => RenderbufferStorage.Rgb8ui,
                //DataFormat.BGR8i => RenderbufferStorage.Rgb8i,
                //DataFormat.BGR8srgb => RenderbufferStorage.Srgb8,

                //DataFormat.RGBA8un => RenderbufferStorage.Rgba8Snorm,
                DataFormat.RGBA8n => RenderbufferStorage.Rgba8,
                DataFormat.RGBA8us => RenderbufferStorage.Rgba8,
                DataFormat.RGBA8s => RenderbufferStorage.Rgba8,
                DataFormat.RGBA8ui => RenderbufferStorage.Rgba8ui,
                DataFormat.RGBA8i => RenderbufferStorage.Rgba8i,
                DataFormat.RGBA8srgb => RenderbufferStorage.Srgb8Alpha8,

                //DataFormat.BGRA8un => RenderbufferStorage.Rgba8Snorm,
                //DataFormat.BGRA8n => RenderbufferStorage.Rgba8,
                //DataFormat.BGRA8us => RenderbufferStorage.Rgba8,
                //DataFormat.BGRA8s => RenderbufferStorage.Rgba8,
                //DataFormat.BGRA8ui => RenderbufferStorage.Rgba8ui,
                //DataFormat.BGRA8i => RenderbufferStorage.Rgba8i,
                //DataFormat.BGRA8srgb => RenderbufferStorage.Srgb8Alpha8,


                //DataFormat.R16un => RenderbufferStorage.R16Snorm,
                DataFormat.R16n => RenderbufferStorage.R16,
                DataFormat.R16us => RenderbufferStorage.R16,
                DataFormat.R16s => RenderbufferStorage.R16,
                DataFormat.R16ui => RenderbufferStorage.R16ui,
                DataFormat.R16i => RenderbufferStorage.R16i,
                DataFormat.R16f => RenderbufferStorage.R16f,

                //DataFormat.RG16un => RenderbufferStorage.Rg16Snorm,
                DataFormat.RG16n => RenderbufferStorage.Rg16,
                DataFormat.RG16us => RenderbufferStorage.Rg16,
                DataFormat.RG16s => RenderbufferStorage.Rg16,
                DataFormat.RG16ui => RenderbufferStorage.Rg16ui,
                DataFormat.RG16i => RenderbufferStorage.Rg16i,
                DataFormat.RG16f => RenderbufferStorage.Rg16f,

                //DataFormat.RGB16un => RenderbufferStorage.Rgb16Snorm,
                DataFormat.RGB16n => RenderbufferStorage.Rgb16,
                DataFormat.RGB16us => RenderbufferStorage.Rgb16,
                DataFormat.RGB16s => RenderbufferStorage.Rgb16,
                DataFormat.RGB16ui => RenderbufferStorage.Rgb16ui,
                DataFormat.RGB16i => RenderbufferStorage.Rgb16i,
                DataFormat.RGB16f => RenderbufferStorage.Rgb16f,

                //DataFormat.RGBA16un => RenderbufferStorage.Rgba16Snorm,
                DataFormat.RGBA16n => RenderbufferStorage.Rgba16,
                DataFormat.RGBA16us => RenderbufferStorage.Rgba16,
                DataFormat.RGBA16s => RenderbufferStorage.Rgba16,
                DataFormat.RGBA16ui => RenderbufferStorage.Rgba16ui,
                DataFormat.RGBA16i => RenderbufferStorage.Rgba16i,
                DataFormat.RGBA16f => RenderbufferStorage.Rgba16f,


                DataFormat.R32ui => RenderbufferStorage.R32ui,
                DataFormat.R32i => RenderbufferStorage.R32i,
                DataFormat.R32f => RenderbufferStorage.R32f,

                DataFormat.RG32ui => RenderbufferStorage.Rg32ui,
                DataFormat.RG32i => RenderbufferStorage.Rg32i,
                DataFormat.RG32f => RenderbufferStorage.Rg32f,

                DataFormat.RGB32ui => RenderbufferStorage.Rgb32ui,
                DataFormat.RGB32i => RenderbufferStorage.Rgb32i,
                DataFormat.RGB32f => RenderbufferStorage.Rgb32f,

                DataFormat.RGBA32ui => RenderbufferStorage.Rgba32ui,
                DataFormat.RGBA32i => RenderbufferStorage.Rgba32i,
                DataFormat.RGBA32f => RenderbufferStorage.Rgba32f,


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


                DataFormat.Depth16un => RenderbufferStorage.DepthComponent16,
                DataFormat.Depth24un => RenderbufferStorage.DepthComponent24,
                DataFormat.Depth32f => RenderbufferStorage.DepthComponent32f,
                DataFormat.Stencil8ui => RenderbufferStorage.DepthStencil,
                DataFormat.Depth16un_Stencil8ui => throw new NotSupportedException($"{format} is not supported in OpenGL!"),
                DataFormat.Depth24un_Stencil8ui => RenderbufferStorage.Depth24Stencil8,
                DataFormat.Depth32f_Stencil8ui => RenderbufferStorage.Depth32fStencil8,

                DataFormat.Undefined => 0,
                _ => 0,
            };


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRenderBuffer(this DataFormat format, TextureType textureType, in MemoryType memoryType, uint mipLevels)
            => textureType.IsRenderBuffer(memoryType, mipLevels) && format.ToRenderbufferStorage() != 0;

        public static ClearBufferMask ToClearBufferMask(this IGLFrameBuffer.BlitMask blitMask)
        {
            ClearBufferMask result = ClearBufferMask.None;

            if (blitMask.HasFlag(IGLFrameBuffer.BlitMask.Color)) result |= ClearBufferMask.ColorBufferBit;
            if (blitMask.HasFlag(IGLFrameBuffer.BlitMask.Depth)) result |= ClearBufferMask.DepthBufferBit;
            if (blitMask.HasFlag(IGLFrameBuffer.BlitMask.Stencil)) result |= ClearBufferMask.StencilBufferBit;

            return result;
        }

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
                TextureWrapType.MirrorRepeat => TextureWrapMode.MirroredRepeat,
                TextureWrapType.ClampToEdge => TextureWrapMode.ClampToEdge,
                TextureWrapType.ClampToBorder => TextureWrapMode.ClampToBorder,
                TextureWrapType.MirrorClampToEdge => (TextureWrapMode)34627, //GL_MIRROR_CLAMP_TO_EDGE (GL 4.4 and above)
                _ => TextureWrapMode.Repeat,
            };

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
                BlendFactor.DualSourceColor => BlendingFactorSrc.Src1Color,
                BlendFactor.OneMinusDualSourceColor => BlendingFactorSrc.OneMinusSrc1Color,
                BlendFactor.DualSourceAlpha => BlendingFactorSrc.Src1Alpha,
                BlendFactor.OneMinusDualSourceAlpha => BlendingFactorSrc.OneMinusSrc1Alpha,
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
                BlendFactor.DualSourceColor => BlendingFactorDest.Src1Color,
                BlendFactor.OneMinusDualSourceColor => BlendingFactorDest.OneMinusSrc1Color,
                BlendFactor.DualSourceAlpha => BlendingFactorDest.Src1Alpha,
                BlendFactor.OneMinusDualSourceAlpha => BlendingFactorDest.OneMinusSrc1Alpha,
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

        public static OpenTK.Graphics.OpenGL.PolygonMode ToPolygonMode(this PolygonMode polygonMode)
            => polygonMode switch
            {
                PolygonMode.Fill => OpenTK.Graphics.OpenGL.PolygonMode.Fill,
                PolygonMode.Line => OpenTK.Graphics.OpenGL.PolygonMode.Line,
                PolygonMode.Point => OpenTK.Graphics.OpenGL.PolygonMode.Point,
                _ => OpenTK.Graphics.OpenGL.PolygonMode.Fill,
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


        [Conditional("DEBUG")]
        public static void DebugWriteGLErrors(string location)
        {
            ErrorCode error;
            while (true)
            {
                error = GL.GetError();
                if (error != ErrorCode.NoError)
                    Debug.WriteLine($"OpenGL Core ERROR - {location} - {error}");
                else break;
            }
        }
        [Conditional("DEBUG")]
        public static void DebugWriteGLErrors(string location, OpenGL.Commands.IGLCommand command)
        {
            ErrorCode error;
            while (true)
            {
                error = GL.GetError();
                if (error != ErrorCode.NoError)
                    Debug.WriteLine($"OpenGL Core ERROR - {location} - {command} - {error}");
                else break;
            }
        }

    }
}
