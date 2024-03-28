using OpenTK.Graphics.ES30;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLES30
{
    internal sealed class GLES30FrameBuffer<T, GLT> : GLFrameBuffer<T, GLT> where T : ITexture where GLT : GLES30Texture, T
    {

        #region Fields

        private ClearBufferMask _glReadBlitMask;
        private ClearBufferMask _glWriteBlitMask;

        #endregion

        #region Constructors

        internal GLES30FrameBuffer(GLGraphicsDevice device, IRenderPass renderPass, DefaultFrameBufferOption defaultFBOOption, in ReadOnlySpan<T> images, Vector2UInt resolution, uint layers, bool ownImages) : base(device, renderPass, defaultFBOOption, images, resolution, layers, ownImages)
        {
            _glReadBlitMask = _readBlitMask.ToClearBufferMask();
            _glWriteBlitMask = _writeBlitMask.ToClearBufferMask();
        }

        #endregion

        #region Public Methods

        public override void GLInitialize()
        {
            ReadOnlySpan<RenderPassStep> steps = _renderPass.Steps;

            int fbosToCreacte = _defaultFBOOption == DefaultFrameBufferOption.LastIsDefaultFBO ? (steps.Length - 1) : steps.Length;
            if (fbosToCreacte > 0)
            {
                GL.GenFramebuffers(fbosToCreacte, _ids);

                for (int i = 0; i < fbosToCreacte; i++)
                {
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, _ids[i]);

                    //Color Attachments
                    ReadOnlySpan<uint> colorAttachmentIndices = steps[i].ColorAttachmentIndices;
                    if (colorAttachmentIndices.Length > 0)
                    {
                        Span<DrawBufferMode> modes = stackalloc DrawBufferMode[colorAttachmentIndices.Length];
                        int colorAttachmentIndex = 0;
                        foreach (int attachmentIndex in colorAttachmentIndices)
                        {
                            Unsafe.As<IGLTexture>(_images[attachmentIndex]).GLBindToFrameBuffer(_ids[i], AttachmentType.Color, colorAttachmentIndex);
                            modes[colorAttachmentIndex] = DrawBufferMode.ColorAttachment0 + (colorAttachmentIndex++);
                        }
                        GL.DrawBuffers(modes.Length, ref modes[0]);
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
                    FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
                    if (status != FramebufferErrorCode.FramebufferComplete)
                        Debug.Fail($"GLES30FrameBuffer.Initialize - FBO Incomplete: {status}!");

                }

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
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _ids[_ids.Length - 1]);
            GL.BlitFramebuffer(0, 0, (int)Resolution.x, (int)Resolution.y, 0, 0, (int)Resolution.x, (int)Resolution.y, _glReadBlitMask, BlitFramebufferFilter.Nearest);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        }
        public override void GLBlitToDefault()
        {
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _ids[_ids.Length - 1]);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            GL.BlitFramebuffer(0, 0, (int)Resolution.x, (int)Resolution.y, 0, 0, (int)Resolution.x, (int)Resolution.y, _glWriteBlitMask, BlitFramebufferFilter.Nearest);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
        }

        #endregion

    }
}
