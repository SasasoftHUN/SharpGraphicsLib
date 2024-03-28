using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using OpenTK.Graphics.OpenGL;
using SharpGraphics.Utils;

namespace SharpGraphics.OpenGL.OpenGLCore
{
    internal sealed class GLCoreFrameBuffer<T, GLT> : GLFrameBuffer<T, GLT> where T : ITexture where GLT : GLCoreTexture, T
    {

        #region Fields

        private ClearBufferMask _glReadBlitMask;
        private ClearBufferMask _glWriteBlitMask;

        #endregion

        #region Constructors

        internal GLCoreFrameBuffer(GLGraphicsDevice device, IRenderPass renderPass, DefaultFrameBufferOption defaultFBOOption, in ReadOnlySpan<T> images, Vector2UInt resolution, uint layers, bool ownImages) :
            base(device, renderPass, defaultFBOOption, images, resolution, layers, ownImages)
        {
            _glReadBlitMask = _readBlitMask.ToClearBufferMask();
            _glWriteBlitMask = _writeBlitMask.ToClearBufferMask();
        }

        #endregion

        #region Protected Methods


        #endregion

        #region Public Methods

        public override void GLInitialize()
        {
            ReadOnlySpan<RenderPassStep> steps = _renderPass.Steps;

            int fbosToCreacte = _defaultFBOOption == DefaultFrameBufferOption.LastIsDefaultFBO ? (steps.Length - 1) : steps.Length;
            if (fbosToCreacte > 0)
            {
                bool dsa = _device.GLFeatures.IsDirectStateAccessSupported;
                
                if (dsa)
                    GL.CreateFramebuffers(fbosToCreacte, _ids);
                else GL.GenFramebuffers(fbosToCreacte, _ids);

                for (int i = 0; i < fbosToCreacte; i++)
                {
                    if (!dsa)
                        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _ids[i]);

                    //Color Attachments
                    ReadOnlySpan<uint> colorAttachmentIndices = steps[i].ColorAttachmentIndices;
                    if (colorAttachmentIndices.Length > 0)
                    {
                        Span<DrawBuffersEnum> modes = stackalloc DrawBuffersEnum[colorAttachmentIndices.Length];
                        int colorAttachmentIndex = 0;
                        foreach (int attachmentIndex in colorAttachmentIndices)
                        {
                            IGLTexture glTexture = Unsafe.As<IGLTexture>(_images[attachmentIndex]);
                            glTexture.GLBindToFrameBuffer(_ids[i], AttachmentType.Color, colorAttachmentIndex);
                            modes[colorAttachmentIndex] = DrawBuffersEnum.ColorAttachment0 + (colorAttachmentIndex++);
                        }

                        if (dsa)
                            GL.NamedFramebufferDrawBuffers(_ids[i], modes.Length, ref modes[0]);
                        else GL.DrawBuffers(modes.Length, ref modes[0]);
                    }

                    //Depth-Stencil Attachment
                    if (steps[i].HasDepthStencilAttachment)
                    {
                        IGLTexture glTexture = Unsafe.As<IGLTexture>(_images[steps[i].DepthStencilAttachmentIndex]);
                        switch (glTexture.DataFormat.ToSwapChainAttachmentType())
                        {
                            case AttachmentType.Depth:
                                glTexture.GLBindToFrameBuffer(_ids[i], AttachmentType.Depth, 0);
                                break;
                            case AttachmentType.Stencil:
                                glTexture.GLBindToFrameBuffer(_ids[i], AttachmentType.Stencil, 0);
                                break;
                            case AttachmentType.DepthStencil:
                                glTexture.GLBindToFrameBuffer(_ids[i], AttachmentType.Depth | AttachmentType.Stencil, 0);
                                break;
                        }
                    }

                    //Completeness check
                    FramebufferErrorCode status = dsa ? (FramebufferErrorCode)GL.CheckNamedFramebufferStatus(_ids[i], FramebufferTarget.Framebuffer) : GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                    if (status != FramebufferErrorCode.FramebufferComplete)
                        Debug.Fail($"GLCoreFrameBuffer.Initialize - FBO Incomplete: {status}!");

                }

                if (!dsa)
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            }
        }
        public override void GLFree()
        {
            int fbosToDelete = _ids[_ids.Length - 1] == 0 ? _ids.Length - 1 : _ids.Length;
            if (fbosToDelete > 0)
                GL.DeleteFramebuffers(fbosToDelete, _ids);
        }

        public override void GLBind(int index) => GL.BindFramebuffer(FramebufferTarget.Framebuffer, _ids[index]);
        public override void GLUnBind() => GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        public override void GLBlitFromDefault()
        {
            if (_device.GLFeatures.IsDirectStateAccessSupported)
                GL.BlitNamedFramebuffer(0, _ids[_ids.Length - 1], 0, 0, (int)Resolution.x, (int)Resolution.y, 0, 0, (int)Resolution.x, (int)Resolution.y, _glReadBlitMask, BlitFramebufferFilter.Nearest);
            else
            {
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _ids[_ids.Length - 1]);
                GL.BlitFramebuffer(0, 0, (int)Resolution.x, (int)Resolution.y, 0, 0, (int)Resolution.x, (int)Resolution.y, _glReadBlitMask, BlitFramebufferFilter.Nearest);
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            }
        }
        public override void GLBlitToDefault()
        {
            if (_device.GLFeatures.IsDirectStateAccessSupported)
                GL.BlitNamedFramebuffer(_ids[_ids.Length - 1], 0, 0, 0, (int)Resolution.x, (int)Resolution.y, 0, 0, (int)Resolution.x, (int)Resolution.y, _glWriteBlitMask, BlitFramebufferFilter.Nearest);
            else
            {
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _ids[_ids.Length - 1]);
                GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
                GL.BlitFramebuffer(0, 0, (int)Resolution.x, (int)Resolution.y, 0, 0, (int)Resolution.x, (int)Resolution.y, _glWriteBlitMask, BlitFramebufferFilter.Nearest);
                GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            }
        }

        #endregion

    }
}
