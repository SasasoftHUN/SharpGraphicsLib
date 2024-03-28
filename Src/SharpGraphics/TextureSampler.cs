using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace SharpGraphics
{

    public enum TextureFilterType
    {
        Nearest, Linear,
        //TODO: VK: CubicIMG, CubicEXT; GL: Filter4Sgis, PixelTexGenQCeilingSgix, PixelTexGenQRoundSgix, PixelTexGenQFloorSgix
    }
    public enum TextureMipMapType
    {
        NotUsed, Nearest, Linear
    }
    public enum TextureWrapType
    {
        Repeat, MirrorRepeat,
        ClampToEdge, ClampToBorder,
        MirrorClampToEdge,
        //TODO: VK: MirrorClampToEdgeKHR; GL: ClampToBorderArb, ClampToBorderNv, ClampToBorderSgis, ClampToEdgeSgis
    }
    public readonly struct TextureWrap
    {
        public readonly TextureWrapType u;
        public readonly TextureWrapType v;
        public readonly TextureWrapType w;

        public TextureWrap(TextureWrapType wrap)
        {
            u = wrap;
            v = wrap;
            w = wrap;
        }
        public TextureWrap(TextureWrapType u, TextureWrapType v, TextureWrapType w)
        {
            this.u = u;
            this.v = v;
            this.w = w;
        }
    }
    public readonly struct TextureMipMapMode
    {
        public readonly TextureMipMapType mode;
        public readonly TextureRangeF lodRange;
        public readonly float lodBias;

        public TextureMipMapMode(TextureMipMapType mode)
        {
            this.mode = mode;
            lodRange = mode == TextureMipMapType.NotUsed ? new TextureRangeF(0f, 0f) : new TextureRangeF(0f, float.MaxValue);
            lodBias = 0f;
        }
        public TextureMipMapMode(TextureMipMapType mode, TextureRangeF range)
        {
            this.mode = mode;
            lodRange = mode == TextureMipMapType.NotUsed ? new TextureRangeF(0f, 0f) : range;
            lodBias = 0f;
        }
        public TextureMipMapMode(TextureMipMapType mode, float bias)
        {
            this.mode = mode;
            if (mode == TextureMipMapType.NotUsed)
            {
                lodRange = new TextureRangeF(0f, 0f);
                lodBias = 0f;
            }
            else
            {
                lodRange = new TextureRangeF(0f, float.MaxValue);
                lodBias = bias;
            }
        }
        public TextureMipMapMode(TextureMipMapType mode, TextureRangeF range, float bias)
        {
            this.mode = mode;
            if (mode == TextureMipMapType.NotUsed)
            {
                lodRange = new TextureRangeF(0f, 0f);
                lodBias = 0f;
            }
            else
            {
                lodRange = range;
                lodBias = bias;
            }
        }
    }

    public readonly struct TextureSamplerConstruction
    {
        public readonly TextureFilterType magnifyingFilter;
        public readonly TextureFilterType minifyingFilter;
        public readonly float anisotropy;
        public readonly TextureMipMapMode mipmapMode;
        public readonly TextureWrap wrap;
        //TODO: public readonly Vector4 borderColor;

        //TODO: Depth Compare Operation? https://www.khronos.org/registry/vulkan/specs/1.3-extensions/html/vkspec.html#textures-depth-compare-operation
        //TODO: unnormalizedCoordinates?

        public TextureSamplerConstruction(TextureFilterType filter = TextureFilterType.Nearest, TextureMipMapType mipmapMode = TextureMipMapType.NotUsed, float anisotropy = 1f, TextureWrapType wrap = TextureWrapType.Repeat)
        {
            this.magnifyingFilter = filter;
            this.minifyingFilter = filter;
            this.anisotropy = anisotropy >= 1f ? anisotropy : 1f;
            this.mipmapMode = new TextureMipMapMode(mipmapMode);
            this.wrap = new TextureWrap(wrap);
            //this.borderColor = new Vector4(0f);
        }
        public TextureSamplerConstruction(TextureFilterType magnifyingFilter, TextureFilterType minifyingFilter, in TextureMipMapMode mipMapMode, float anisotropy,
            in TextureWrap wrap)//, Vector4 borderColor)
        {
            this.magnifyingFilter = magnifyingFilter;
            this.minifyingFilter = minifyingFilter;
            this.anisotropy = anisotropy >= 1f ? anisotropy : 1f;
            this.mipmapMode = mipMapMode;
            this.wrap = wrap;
            //this.borderColor = borderColor;
        }
    }

    public abstract class TextureSampler : IDisposable
    {

        #region Fields

        private bool _isDisposed;

        #endregion

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null
                _isDisposed = true;
            }
        }

        #endregion

        #region Public Methods

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
