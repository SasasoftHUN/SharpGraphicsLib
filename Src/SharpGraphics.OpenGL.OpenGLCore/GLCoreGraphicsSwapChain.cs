using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
#if OPENTK4
using OpenTK.Windowing.Common;
#endif
using SharpGraphics.GraphicsViews;
using SharpGraphics.OpenGL.Commands;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLCore
{
    internal sealed class GLCoreGraphicsSwapChain : GLGraphicsSwapChain
    {

        #region Fields

        private readonly GLCoreGraphicsDevice _device;

        private readonly bool _isDoubleBuffered;
        private readonly bool _isSRGB;

        #endregion

        #region Properties

        public override bool IsDoubleBuffered => _isDoubleBuffered;

        #endregion

        #region Constructors

        internal GLCoreGraphicsSwapChain(GLCoreGraphicsDevice device, IGraphicsView view, GLCommandProcessor presentProcessor) :
            base(view, presentProcessor)
        {
            _device = device;

            PixelFormat glFormat = (PixelFormat)GL.GetInteger(GetPName.ImplementationColorReadFormat);
            PixelType glType = (PixelType)GL.GetInteger(GetPName.ImplementationColorReadType);
            GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.Depth, FramebufferParameterName.FramebufferAttachmentDepthSize, out int glDepth);
            GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.Stencil, FramebufferParameterName.FramebufferAttachmentStencilSize, out int glStencil);
            _isDoubleBuffered = GL.GetBoolean(GetPName.Doublebuffer);
            bool isSRGBCapable = GL.GetBoolean(/*GL_FRAMEBUFFER_SRGB_CAPABLE*/(GetPName)0x8DBA);

            SwapChainConstruction swapChainConstruction = view.SwapChainConstructionRequest;
            _isSRGB = false;
            if (swapChainConstruction.colorFormat.IsSRGB())
            {
                if (isSRGBCapable)
                {
                    GL.Enable(EnableCap.FramebufferSrgb);
                    _isSRGB = true;
                }
            }
            else GL.Disable(EnableCap.FramebufferSrgb);

            DataFormat glColorFormat = glFormat.ToDataFormat(glType);
            if (_isSRGB)
                glColorFormat = glColorFormat.ToSRGB();
            CheckForFormatFallback(new SwapChainConstruction(view.SwapChainConstructionRequest.mode, glColorFormat, glDepth.ToDataFormat(glStencil)));
        }

        #endregion

        #region Protected Methods

        protected override GLFrameBuffer<ITexture2D, IGLTexture2D> CreateFrameBuffer()
        {
            if (RenderPass == null)
                throw new NullReferenceException("GLCoreGraphicsSwapChain has no RenderPass");

            ReadOnlySpan<RenderPassAttachment> attachments = RenderPass.Attachments;
            IGLTexture2D[] images = new IGLTexture2D[attachments.Length];
            RenderPassStep lastStep = RenderPass.Steps[RenderPass.Steps.Length - 1];

            if (IsDefaultFBOCompatible(lastStep, attachments))
            {
                for (int i = 0; i < attachments.Length; i++)
                    if (!lastStep.IsWritingAttachment((uint)i))
                        images[i] = Unsafe.As<IGLTexture2D>(_device.CreateTexture2D(attachments[i].format, Size, attachments[i].type.ToTextureType(), MemoryType.DeviceOnly, 1u));
                    //TODO: else images[i] = placeholder texture
                return Unsafe.As<GLFrameBuffer<ITexture2D, IGLTexture2D>>(new GLCoreFrameBuffer<ITexture2D, GLCoreTexture2D>(_device, RenderPass, DefaultFrameBufferOption.LastIsDefaultFBO, images, Size, 1u, true));
            }
            else
            {
                for (int i = 0; i < attachments.Length; i++)
                    images[i] = Unsafe.As<IGLTexture2D>(_device.CreateTexture2D(attachments[i].format, Size, attachments[i].type.ToTextureType(), MemoryType.DeviceOnly, 1u));
                return Unsafe.As<GLFrameBuffer<ITexture2D, IGLTexture2D>>(new GLCoreFrameBuffer<ITexture2D, GLCoreTexture2D>(_device, RenderPass, DefaultFrameBufferOption.NeedToBlitDefault, images, Size, 1u, true));
            }
        }

        #endregion

    }
}
