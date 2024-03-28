using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics.Utils
{
    public static class GraphicsUtils
    {

        public static int GetFormatBytes(this DataFormat format)
            => format switch
            {
                DataFormat.R8un => 1,
                DataFormat.R8n => 1,
                DataFormat.R8us => 1,
                DataFormat.R8s => 1,
                DataFormat.R8ui => 1,
                DataFormat.R8i => 1,
                DataFormat.R8srgb => 1,

                DataFormat.RG8un => 2,
                DataFormat.RG8n => 2,
                DataFormat.RG8us => 2,
                DataFormat.RG8s => 2,
                DataFormat.RG8ui => 2,
                DataFormat.RG8i => 2,
                DataFormat.RG8srgb => 2,

                DataFormat.RGB8un => 3,
                DataFormat.RGB8n => 3,
                DataFormat.RGB8us => 3,
                DataFormat.RGB8s => 3,
                DataFormat.RGB8ui => 3,
                DataFormat.RGB8i => 3,
                DataFormat.RGB8srgb => 3,
                DataFormat.BGR8un => 3,
                DataFormat.BGR8n => 3,
                DataFormat.BGR8us => 3,
                DataFormat.BGR8s => 3,
                DataFormat.BGR8ui => 3,
                DataFormat.BGR8i => 3,
                DataFormat.BGR8srgb => 3,

                DataFormat.RGBA8un => 4,
                DataFormat.RGBA8n => 4,
                DataFormat.RGBA8us => 4,
                DataFormat.RGBA8s => 4,
                DataFormat.RGBA8ui => 4,
                DataFormat.RGBA8i => 4,
                DataFormat.RGBA8srgb => 4,
                DataFormat.BGRA8un => 4,
                DataFormat.BGRA8n => 4,
                DataFormat.BGRA8us => 4,
                DataFormat.BGRA8s => 4,
                DataFormat.BGRA8ui => 4,
                DataFormat.BGRA8i => 4,
                DataFormat.BGRA8srgb => 4,


                DataFormat.R16un => 2,
                DataFormat.R16n => 2,
                DataFormat.R16us => 2,
                DataFormat.R16s => 2,
                DataFormat.R16ui => 2,
                DataFormat.R16i => 2,
                DataFormat.R16f => 2,

                DataFormat.RG16un => 4,
                DataFormat.RG16n => 4,
                DataFormat.RG16us => 4,
                DataFormat.RG16s => 4,
                DataFormat.RG16ui => 4,
                DataFormat.RG16i => 4,
                DataFormat.RG16f => 4,

                DataFormat.RGB16un => 6,
                DataFormat.RGB16n => 6,
                DataFormat.RGB16us => 6,
                DataFormat.RGB16s => 6,
                DataFormat.RGB16ui => 6,
                DataFormat.RGB16i => 6,
                DataFormat.RGB16f => 6,

                DataFormat.RGBA16un => 8,
                DataFormat.RGBA16n => 8,
                DataFormat.RGBA16us => 8,
                DataFormat.RGBA16s => 8,
                DataFormat.RGBA16ui => 8,
                DataFormat.RGBA16i => 8,
                DataFormat.RGBA16f => 8,


                DataFormat.R32ui => 4,
                DataFormat.R32i => 4,
                DataFormat.R32f => 4,

                DataFormat.RG32ui => 8,
                DataFormat.RG32i => 8,
                DataFormat.RG32f => 8,

                DataFormat.RGB32ui => 12,
                DataFormat.RGB32i => 12,
                DataFormat.RGB32f => 12,

                DataFormat.RGBA32ui => 16,
                DataFormat.RGBA32i => 16,
                DataFormat.RGBA32f => 16,


                DataFormat.R64ui => 8,
                DataFormat.R64i => 8,
                DataFormat.R64f => 8,

                DataFormat.RG64ui => 16,
                DataFormat.RG64i => 16,
                DataFormat.RG64f => 16,

                DataFormat.RGB64ui => 24,
                DataFormat.RGB64i => 24,
                DataFormat.RGB64f => 24,

                DataFormat.RGBA64ui => 32,
                DataFormat.RGBA64i => 32,
                DataFormat.RGBA64f => 32,


                DataFormat.Depth16un => 2,
                DataFormat.Depth24un => 3,
                DataFormat.Depth32f => 4,
                DataFormat.Stencil8ui => 1,
                DataFormat.Depth16un_Stencil8ui => 3, //Is it packed?
                DataFormat.Depth24un_Stencil8ui => 4,
                DataFormat.Depth32f_Stencil8ui => 8, //Is it packed?

                _ => 0,
            };
        public static bool IsSRGB(this DataFormat dataFormat)
            => dataFormat switch
            {
                DataFormat.R8srgb => true,
                DataFormat.RG8srgb => true,
                DataFormat.RGB8srgb => true,
                DataFormat.BGR8srgb => true,
                DataFormat.RGBA8srgb => true,
                DataFormat.BGRA8srgb => true,

                _ => false,
            };
        public static DataFormat ToSRGB(this DataFormat dataFormat)
            => dataFormat switch
            {
                DataFormat.R8un => DataFormat.R8srgb,
                DataFormat.RG8un => DataFormat.RG8srgb,
                DataFormat.RGB8un => DataFormat.RGB8srgb,
                DataFormat.BGR8un => DataFormat.BGR8srgb,
                DataFormat.RGBA8un => DataFormat.RGBA8srgb,
                DataFormat.BGRA8un => DataFormat.BGRA8srgb,

                _ => dataFormat,
            };
        public static DataFormat FromSRGB(this DataFormat dataFormat)
            => dataFormat switch
            {
                DataFormat.R8srgb => DataFormat.R8un,
                DataFormat.RG8srgb => DataFormat.RG8un,
                DataFormat.RGB8srgb => DataFormat.RGB8un,
                DataFormat.BGR8srgb => DataFormat.BGR8un,
                DataFormat.RGBA8srgb => DataFormat.RGBA8un,
                DataFormat.BGRA8srgb => DataFormat.BGRA8un,

                _ => dataFormat,
            };
        public static AttachmentType ToSwapChainAttachmentType(this DataFormat dataFormat)
            => dataFormat switch
            {
                DataFormat.Undefined => AttachmentType.Undefined,

                DataFormat.Depth16un => AttachmentType.Depth,
                DataFormat.Depth24un => AttachmentType.Depth,
                DataFormat.Depth32f => AttachmentType.Depth,
                DataFormat.Stencil8ui => AttachmentType.Stencil,
                DataFormat.Depth16un_Stencil8ui => AttachmentType.DepthStencil,
                DataFormat.Depth24un_Stencil8ui => AttachmentType.DepthStencil,
                DataFormat.Depth32f_Stencil8ui => AttachmentType.DepthStencil,

                _ => AttachmentType.Color,
            };


        public static TextureType ToTextureType(this AttachmentType attachmentType)
        {
            TextureType result = TextureType.Unknown;

            if (attachmentType.HasFlag(AttachmentType.Color)) result |= TextureType.ColorAttachment;
            if (attachmentType.HasFlag(AttachmentType.Depth) || attachmentType.HasFlag(AttachmentType.Stencil)) result |= TextureType.DepthStencilAttachment;
            if (attachmentType.HasFlag(AttachmentType.ShaderInput)) result |= TextureType.InputAttachment;
            //Present must be combined with color, so it's handled there

            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint CalculateMipLevels(this uint width)
            => (uint)Math.Floor(Math.Log(width, 2u)) + 1u;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint CalculateMipLevels(this Vector2UInt resolution)
            => (uint)Math.Floor(Math.Log(Math.Min(resolution.x, resolution.y), 2u)) + 1u;
        /*public static uint CalculateMipLevels(this Vector3UInt resolution)
            => (uint)Math.Floor(Math.Log(Math.Min(Math.Min(resolution.x, resolution.y), resolution.z, 2))) + 1;*/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint CalculateMipLevels(this uint mipLevels, uint width)
            => mipLevels == 0u || mipLevels == uint.MaxValue ? width.CalculateMipLevels() : mipLevels;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint CalculateMipLevels(this uint mipLevels, Vector2UInt resolution)
            => mipLevels == 0u || mipLevels == uint.MaxValue ? resolution.CalculateMipLevels() : mipLevels;
        /*public static uint CalculateMipLevels(this uint mipLevels, Vector3UInt resolution)
            => mipLevels == 0u || mipLevels == uint.MaxValue ? resolution.CalculateMipLevels() : mipLevels;*/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint CalculateMipLevelResolution(this uint mipLevel, uint width) => mipLevel == 0u ? width : width / (uint)MathF.Pow(2f, mipLevel);
        public static Vector2UInt CalculateMipLevelResolution(this uint mipLevel, Vector2UInt resolution)
        {
            if (mipLevel == 0u)
                return resolution;
            else
            {
                float divider = 1f / MathF.Pow(2f, mipLevel);
                return new Vector2UInt(
                    Math.Max((uint)MathF.Round(resolution.x * divider), 1u),
                    Math.Max((uint)MathF.Round(resolution.y * divider), 1u));
            }
        }
        public static Vector3UInt CalculateMipLevelExtent(this uint mipLevel, Vector3UInt extent)
        {
            if (mipLevel == 0u)
                return extent;
            else
            {
                float divider = 1f / MathF.Pow(2f, mipLevel);
                return new Vector3UInt(
                    Math.Max((uint)MathF.Round(extent.x * divider), 1u),
                    Math.Max((uint)MathF.Round(extent.y * divider), 1u),
                    Math.Max((uint)MathF.Round(extent.z * divider), 1u));
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint CalculateMipLevelTotalPixelCount(this uint mipLevel, Vector2UInt resolution, uint layers)
        {
            Vector2UInt mipResolution = CalculateMipLevelResolution(mipLevel, resolution);
            return mipResolution.x * mipResolution.y * layers;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint CalculateMipLevelsTotalPixelCount(this TextureRange mipLevels, Vector2UInt resolution, uint layers)
        {
            uint totalPixelCount = 0u;
            for (uint i = 0u; i < mipLevels.count; i++)
            {
                Vector2UInt mipResolution = CalculateMipLevelResolution(mipLevels.start + i, resolution);
                totalPixelCount += mipResolution.x * mipResolution.y * layers;
            }
            return totalPixelCount;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint CalculateMipLevelTotalPixelCount(this uint mipLevel, Vector3UInt extent, uint layers)
        {
            Vector3UInt mipExtent = CalculateMipLevelExtent(mipLevel, extent);
            return mipExtent.x * mipExtent.y * mipExtent.z * layers;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint CalculateMipLevelsTotalPixelCount(this TextureRange mipLevels, Vector3UInt extent, uint layers)
        {
            uint totalPixelCount = 0u;
            for (uint i = 0u; i < mipLevels.count; i++)
            {
                Vector3UInt mipExtent = CalculateMipLevelExtent(mipLevels.start + i, extent);
                totalPixelCount += mipExtent.x * mipExtent.y * mipExtent.z * layers;
            }
            return totalPixelCount;
        }

    }
}
