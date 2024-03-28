using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL
{
    public abstract class GLRenderPass : RenderPass
    {

        #region Fields

        //private bool _isDisposed;

        protected readonly GLGraphicsDevice _device;

        #endregion

        #region Properties


        #endregion

        #region Constructors

        //This constructor (and the RenderPass itself) doesn't use any GL resources. If that changes it will need a CreateInternal Command.
        protected GLRenderPass(GLGraphicsDevice device, in ReadOnlySpan<RenderPassAttachment> attachments, in ReadOnlySpan<RenderPassStep> steps) : base(attachments, steps)
        {
            //_isDisposed = false;
            _device = device;
        }

        //~GLRenderPass() => Dispose(false);

        #endregion

    }
}
