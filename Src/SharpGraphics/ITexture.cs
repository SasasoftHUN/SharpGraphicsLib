using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGraphics
{

    public enum TextureLayout : uint
    {
        Undefined = 0,
        General = 1,
        ColorAttachment = 2,
        DepthStencilAttachment = 3,
        DepthStencilReadOnly = 4,
        ShaderReadOnly = 5,
        //TransferSrcOptimal = 6,
        //TransferDstOptimal = 7,
        //Preinitialized = 8,
        PresentSrc = 1000001002,
        //SharedPresent = 1000111000,
        //DepthReadOnlyStencilAttachmentOptimal = 1000117000,
        //DepthReadOnlyStencilAttachmentOptimalKHR = 1000117000,
        //DepthAttachmentStencilReadOnlyOptimal = 1000117001,
        //DepthAttachmentStencilReadOnlyOptimalKHR = 1000117001,
        //ShadingRateOptimalNV = 1000164003,
        //FragmentDensityMapOptimalEXT = 1000218000,
        DepthAttachment = 1000241000,
        //DepthAttachmentOptimalKHR = 1000241000,
        //DepthReadOnlyOptimal = 1000241001,
        //DepthReadOnlyOptimalKHR = 1000241001,
        StencilAttachment = 1000241002,
        //StencilAttachmentOptimalKHR = 1000241002,
        //StencilReadOnlyOptimal = 1000241003,
        //StencilReadOnlyOptimalKHR = 1000241003
    }

    [Flags]
    public enum TextureType : uint
    {
        Unknown = 0u,
        Read = 1u, Store = 2u,
        CopySource = 4u, CopyDestination = 8u,
        ShaderSample = 16u, ShaderStorage = 32u,
        ColorAttachment = 64u, DepthStencilAttachment = 128u, InputAttachment = 256u,
    }

    public enum TextureSwizzleType
    {
        Original = 0, Zero = 1, One = 2,
        Red = 3, Green = 4, Blue = 5, Alpha = 6,
    }
    public readonly struct TextureSwizzle
    {
        public readonly TextureSwizzleType red;
        public readonly TextureSwizzleType green;
        public readonly TextureSwizzleType blue;
        public readonly TextureSwizzleType alpha;

        public bool IsOriginal =>
            (red == TextureSwizzleType.Original || red == TextureSwizzleType.Red) &&
            (green == TextureSwizzleType.Original || green == TextureSwizzleType.Green) &&
            (blue == TextureSwizzleType.Original || blue == TextureSwizzleType.Blue) &&
            (alpha == TextureSwizzleType.Original || alpha == TextureSwizzleType.Alpha);

        public TextureSwizzle(TextureSwizzleType swizzle = TextureSwizzleType.Original)
        {
            red = swizzle;
            green = swizzle;
            blue = swizzle;
            alpha = swizzle;
        }
        public TextureSwizzle(TextureSwizzleType red, TextureSwizzleType green, TextureSwizzleType blue)
        {
            this.red = red;
            this.green = green;
            this.blue = blue;
            alpha = TextureSwizzleType.Original;
        }
        public TextureSwizzle(TextureSwizzleType red, TextureSwizzleType green, TextureSwizzleType blue, TextureSwizzleType alpha)
        {
            this.red = red;
            this.green = green;
            this.blue = blue;
            this.alpha = alpha;
        }

        public TextureSwizzleType CombineTypes(TextureSwizzleType oldType, TextureSwizzleType newType)
            => newType switch
            {
                TextureSwizzleType.Original => oldType,
                TextureSwizzleType.Zero => TextureSwizzleType.Zero,
                TextureSwizzleType.One => TextureSwizzleType.One,
                TextureSwizzleType.Red => red == TextureSwizzleType.Original ? TextureSwizzleType.Red : red,
                TextureSwizzleType.Green => green == TextureSwizzleType.Original ? TextureSwizzleType.Green : green,
                TextureSwizzleType.Blue => blue == TextureSwizzleType.Original ? TextureSwizzleType.Blue : blue,
                TextureSwizzleType.Alpha => alpha == TextureSwizzleType.Original ? TextureSwizzleType.Alpha : alpha,
                _ => TextureSwizzleType.Original,
            };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TextureSwizzle Combine(in TextureSwizzle? newest)
            => Combine(newest ?? new TextureSwizzle(TextureSwizzleType.Original));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TextureSwizzle Combine(in TextureSwizzle newest)
            => new TextureSwizzle(
                CombineTypes(red, newest.red),
                CombineTypes(green, newest.green),
                CombineTypes(blue, newest.blue),
                CombineTypes(alpha, newest.alpha)
                );
    }

    public readonly struct TextureRange : IEquatable<TextureRange>
    {

        public readonly uint start;
        public readonly uint count;

        public uint End => start + count - 1u;

        public TextureRange(uint start)
        {
            this.start = start;
            this.count = 1u;
        }
        public TextureRange(uint start, uint count)
        {
            this.start = start;
            this.count = count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInRange(uint x) => x >= start && x < (start + count);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInRange(in TextureRange x) => IsInRange(x.start) && IsInRange(x.End);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TextureRange Combine(in TextureRange? newest) => newest.HasValue ? Combine(newest.Value) : this;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TextureRange Combine(in TextureRange newest) => new TextureRange(start + newest.start, newest.count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TextureRange GetOffset(in TextureRange subRange) => new TextureRange(start, start - subRange.start);

        public static implicit operator TextureRange(int r) => new TextureRange((uint)r, 1u);
        public static implicit operator TextureRange(uint r) => new TextureRange(r, 1u);
        public static explicit operator TextureRangeF(in TextureRange r) => new TextureRangeF(r.start, r.count);

        public static bool operator ==(in TextureRange left, in TextureRange right) => left.Equals(right);
        public static bool operator !=(in TextureRange left, in TextureRange right) => !(left == right);
        public override bool Equals(object? obj) => obj is TextureRange range && Equals(range);
        public bool Equals(TextureRange other) => start == other.start && count == other.count;

        public override int GetHashCode()
        {
            int hashCode = 255766699;
            hashCode = hashCode * -1521134295 + start.GetHashCode();
            hashCode = hashCode * -1521134295 + count.GetHashCode();
            return hashCode;
        }
        public override string ToString() => count == 0u ? "{X}" : (count == 1u ? $"{{{start}}}" : $"{{{start} - {End}}}");

    }
    public readonly struct TextureRangeF
    {
        public readonly float start;
        public readonly float count;

        public float End => start + count;

        public TextureRangeF(float start)
        {
            this.start = start;
            this.count = 0f;
        }
        public TextureRangeF(float start, float count)
        {
            this.start = start;
            this.count = count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInRange(float x) => x >= start && x <= (start + count);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInRange(in TextureRangeF x) => IsInRange(x.start) && IsInRange(x.End);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TextureRangeF Combine(in TextureRangeF? newest) => newest.HasValue ? Combine(newest.Value) : this;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TextureRangeF Combine(in TextureRangeF newest) => new TextureRangeF(start + newest.start, newest.count);

        public static implicit operator TextureRangeF(float r) => new TextureRangeF(r, 0f);
        public static explicit operator TextureRange(in TextureRangeF r) => new TextureRange((uint)r.start, (uint)r.count);

        public override string ToString() => count == 0u ? "{X}" : (count == 1u ? $"{{{start}}}" : $"{{{start} - {End}}}");

    }

    public readonly struct TextureStagingBuffer
    {

        public readonly IStagingDataBuffer stagingBuffer;
        public readonly TextureRange layers;
        public readonly TextureRange levels;
        public readonly uint bufferElementIndexOffset;

        public TextureStagingBuffer(IStagingDataBuffer stagingBuffer, in TextureRange layers, in TextureRange levels)
        {
            this.stagingBuffer = stagingBuffer;
            this.layers = layers;
            this.levels = levels;
            this.bufferElementIndexOffset = 0u;
        }
        public TextureStagingBuffer(IStagingDataBuffer stagingBuffer, in TextureRange layers, in TextureRange levels, uint bufferElementIndexOffset)
        {
            this.stagingBuffer = stagingBuffer;
            this.layers = layers;
            this.levels = levels;
            this.bufferElementIndexOffset = bufferElementIndexOffset;
        }

        public uint GetRequiredElementCount(ITexture texture)
        {
            uint count = 0u;
            for (uint i = 0u; i < levels.count; i++)
            {
                Vector2UInt resolution = (levels.start + i).CalculateMipLevelResolution(texture.Resolution);
                count += resolution.x * resolution.y;
            }
            return count * layers.count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInRange(uint layer, uint level) => layers.IsInRange(layer) && levels.IsInRange(level);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInRange(in TextureRange layer, uint level) => layers.IsInRange(layer) && levels.IsInRange(level);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInRange(uint layer, in TextureRange level) => layers.IsInRange(layer) && levels.IsInRange(level);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsInRange(in TextureRange layer, in TextureRange level) => layers.IsInRange(layer) && levels.IsInRange(level);

        public TextureStagingBuffer AddBufferElementIndexOffset(uint bufferElementIndexOffset)
            => new TextureStagingBuffer(stagingBuffer, layers, levels, this.bufferElementIndexOffset + bufferElementIndexOffset);

    }

    public interface ITexture : IDisposable
    {

        ITexture? ReferenceTexture { get; }
        bool IsView { get; }

        uint Width { get; }
        uint Height { get; }
        Vector2UInt Resolution { get; }
        uint Depth { get; }
        Vector3UInt Extent { get; }
        uint Layers { get; }
        uint MipLevels { get; }
        DataFormat DataFormat { get; }
        TextureType Type { get; }

        bool IsDisposed { get; }

        void GenerateMipmaps(GraphicsCommandBuffer commandBuffer);

        void UseStagingBuffer(IStagingDataBuffer stagingBuffer, uint elementIndexOffset = 0u);
        void UseStagingBuffer(IStagingDataBuffer stagingBuffer, in TextureRange mipLevels, uint elementIndexOffset = 0u);
        void UseStagingBuffer(IStagingDataBuffer stagingBuffer, in TextureRange mipLevels, in TextureRange layers, uint elementIndexOffset = 0u);
        void ReleaseStagingBuffers(bool keepInternalList = false);

        void CopyTo(GraphicsCommandBuffer commandBuffer, IDataBuffer destination, in CopyBufferTextureRange range);
        void CopyTo(GraphicsCommandBuffer commandBuffer, IDataBuffer destination, in ReadOnlySpan<CopyBufferTextureRange> ranges);
        void CopyTo<T>(GraphicsCommandBuffer commandBuffer, IDataBuffer<T> destination, in TextureRange mipLevels, in TextureRange layers, uint destinationElementIndexOffset = 0u) where T : unmanaged;

    }


    public readonly struct CopyBufferTextureRange
    {

        public readonly Vector3UInt extent;

        public readonly uint mipLevel;
        public readonly TextureRange layers;

        public readonly ulong bufferOffset;
        public readonly Vector3UInt textureOffset;

        public CopyBufferTextureRange(in Vector3UInt extent, uint mipLevel, in TextureRange layers)
        {
            this.extent = extent;

            this.mipLevel = mipLevel;
            this.layers = layers;

            bufferOffset = 0ul;
            textureOffset = new Vector3UInt();
        }
        public CopyBufferTextureRange(in Vector3UInt extent, uint mipLevel, in TextureRange layers, ulong bufferOffset, in Vector3UInt textureOffset)
        {
            this.extent = extent;

            this.mipLevel = mipLevel;
            this.layers = layers;

            this.bufferOffset = bufferOffset;
            this.textureOffset = textureOffset;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FillForMultipleMipLevels(in Span<CopyBufferTextureRange> ranges, in Vector3UInt extent, TextureRange mipLevels, in TextureRange layers, ulong bufferOffset, ulong dataTypeSize)
        {
            uint mipLevel;
            ulong offset = bufferOffset;

            for (int i = 0; i < ranges.Length; i++)
            {
                mipLevel = mipLevels.start + (uint)i;
                ranges[i] = new CopyBufferTextureRange(
                    extent: GraphicsUtils.CalculateMipLevelExtent(mipLevel, extent),
                    mipLevel: mipLevel,
                    layers: layers,
                    bufferOffset: offset,
                    textureOffset: new Vector3UInt(0u)
                );
                offset += dataTypeSize * GraphicsUtils.CalculateMipLevelTotalPixelCount(mipLevel, extent, layers.count);
            }
        }

    }

}