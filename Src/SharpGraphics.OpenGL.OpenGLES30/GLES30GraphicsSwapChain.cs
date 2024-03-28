using OpenTK.Graphics;
using SharpGraphics.GraphicsViews;
using SharpGraphics.OpenGL.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.ES30;
using System.Linq;
using SharpGraphics.Utils;
using System.Runtime.CompilerServices;
using OpenTK.Audio.OpenAL;
using System.Data;

namespace SharpGraphics.OpenGL.OpenGLES30
{
    internal sealed class GLES30GraphicsSwapChain : GLGraphicsSwapChain
    {

        #region Fields

        private readonly GLES30GraphicsDevice _device;

        private readonly bool _isDoubleBuffered;

        #endregion

        #region Properties

        public override bool IsDoubleBuffered => _isDoubleBuffered;

        #endregion

        #region Constructors

        internal GLES30GraphicsSwapChain(GLES30GraphicsDevice device, IGraphicsView view, GLCommandProcessor presentProcessor) :
            base(view, presentProcessor)
        {
            _device = device;

            GL.GetInteger(GetPName.ImplementationColorReadFormat, out int pixelFormat);
            PixelFormat glFormat = (PixelFormat)pixelFormat;
            GL.GetInteger(GetPName.ImplementationColorReadType, out int pixelType);
            PixelType glType = (PixelType)pixelType;
#if ANDROID
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.GetInteger(GetPName.DepthBits, out int glDepth);
            //GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferSlot.DepthAttachment, FramebufferParameterName.FramebufferAttachmentDepthSize, out int glDepth);
            GL.GetInteger(GetPName.StencilBits, out int glStencil);
            //GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferSlot.StencilAttachment, FramebufferParameterName.FramebufferAttachmentStencilSize, out int glStencil);
            _isDoubleBuffered = true;
#else
            GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.Depth, FramebufferParameterName.FramebufferAttachmentDepthSize, out int glDepth);
            GL.GetFramebufferAttachmentParameter(FramebufferTarget.Framebuffer, FramebufferAttachment.Stencil, FramebufferParameterName.FramebufferAttachmentStencilSize, out int glStencil);            
            _isDoubleBuffered = GL.GetBoolean(GetPName.Doublebuffer);
#endif
            GL.GetBoolean(/*GL_FRAMEBUFFER_SRGB_CAPABLE*/(GetPName)0x8DBA, out bool isSRGBCapable);

            SwapChainConstruction swapChainConstruction = view.SwapChainConstructionRequest;
            bool isSRGB = false;
            if (swapChainConstruction.colorFormat.IsSRGB())
            {
                if (isSRGBCapable)
                {
                    GL.Enable(/*FRAMEBUFFER_SRGB_EXT*/(EnableCap)0x8DB9); //TODO: Test extensions availability: https://github.com/KhronosGroup/OpenGL-Registry/blob/main/extensions/EXT/EXT_sRGB_write_control.txt
                    isSRGB = true;
                }
            }
            else GL.Disable(/*FRAMEBUFFER_SRGB_EXT*/(EnableCap)0x8DB9);

            DataFormat glColorFormat = glFormat.ToDataFormat(glType);
            if (isSRGB)
                glColorFormat = glColorFormat.ToSRGB();
            CheckForFormatFallback(new SwapChainConstruction(view.SwapChainConstructionRequest.mode, glColorFormat, glDepth.ToDataFormat(glStencil)));
        }

        #endregion

        #region Protected Methods

        protected override GLFrameBuffer<ITexture2D, IGLTexture2D> CreateFrameBuffer()
        {
            if (RenderPass == null)
                throw new NullReferenceException("GLES30GraphicsSwapChain has no RenderPass");

            ReadOnlySpan<RenderPassAttachment> attachments = RenderPass.Attachments;
            IGLTexture2D[] images = new IGLTexture2D[attachments.Length];
            RenderPassStep lastStep = RenderPass.Steps[RenderPass.Steps.Length - 1];

            if (IsDefaultFBOCompatible(lastStep, attachments))
            {
                for (int i = 0; i < attachments.Length; i++)
                    if (!lastStep.IsWritingAttachment((uint)i))
                        images[i] = Unsafe.As<IGLTexture2D>(_device.CreateTexture2D(attachments[i].format, Size, attachments[i].type.ToTextureType(), MemoryType.DeviceOnly));
                    //TODO: else images[i] = placeholder texture
                return Unsafe.As<GLFrameBuffer<ITexture2D, IGLTexture2D>>(new GLES30FrameBuffer<ITexture2D, GLES30Texture2D>(_device, RenderPass, DefaultFrameBufferOption.LastIsDefaultFBO, images, Size, 1u, true));
            }
            else
            {
                for (int i = 0; i < attachments.Length; i++)
                    images[i] = Unsafe.As<IGLTexture2D>(_device.CreateTexture2D(attachments[i].format, Size, attachments[i].type.ToTextureType(), MemoryType.DeviceOnly));
                return Unsafe.As<GLFrameBuffer<ITexture2D, IGLTexture2D>>(new GLES30FrameBuffer<ITexture2D, GLES30Texture2D>(_device, RenderPass, DefaultFrameBufferOption.NeedToBlitDefault, images, Size, 1u, true));
            }
        }

        #endregion

    }
}
