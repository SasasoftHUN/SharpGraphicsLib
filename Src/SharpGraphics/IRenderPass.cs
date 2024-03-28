using System;
using System.Collections.Generic;
using System.Text;
using SharpGraphics.Utils;

namespace SharpGraphics
{
    public interface IRenderPass : IDisposable
    {

        #region Properties

        bool IsDisposed { get; }
        ReadOnlySpan<RenderPassAttachment> Attachments { get; }
        ReadOnlySpan<RenderPassStep> Steps { get; }

        #endregion

        #region Public Methods

        IFrameBuffer<ITexture2D> CreateFrameBuffer2D(in Vector2UInt resolution);
        IFrameBuffer<ITexture2D> CreateFrameBuffer2D(in ReadOnlySpan<ITexture2D> textures);
        
        #endregion

    }
}
