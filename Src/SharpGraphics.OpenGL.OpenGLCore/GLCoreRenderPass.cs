using SharpGraphics.OpenGL.Commands;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLCore
{
    internal sealed class GLCoreRenderPass : GLRenderPass
    {

        #region Fields

        //private bool _isDisposed;

        #endregion

        #region Properties


        #endregion

        #region Constructors

        //This constructor (and the RenderPass itself) doesn't use any GL resources. If that changes it will need a CreateInternal Command.
        internal GLCoreRenderPass(GLCoreGraphicsDevice device, in ReadOnlySpan<RenderPassAttachment> attachments, in ReadOnlySpan<RenderPassStep> steps) : base(device, attachments, steps)
        {
            //_isDisposed = false;
        }

        //~GLCoreRenderPass() => Dispose(false);

        #endregion

        #region Public Methods

        public override IFrameBuffer<ITexture2D> CreateFrameBuffer2D(in Vector2UInt resolution)
        {
            ReadOnlySpan<RenderPassAttachment> attachments = Attachments;
            IGLTexture2D[] images = new IGLTexture2D[attachments.Length];

            for (int i = 0; i < attachments.Length; i++)
                images[i] = Unsafe.As<IGLTexture2D>(_device.CreateTexture2D(attachments[i].format, resolution, attachments[i].type.ToTextureType(), MemoryType.DeviceOnly));

            GLCoreFrameBuffer<ITexture2D, GLCoreTexture2D> frameBuffer = new GLCoreFrameBuffer<ITexture2D, GLCoreTexture2D>(_device, this, DefaultFrameBufferOption.NotUsed, images, resolution, 1u, true);
            _device.InitializeResource(frameBuffer);
            return frameBuffer;
        }

        public override IFrameBuffer<ITexture2D> CreateFrameBuffer2D(in ReadOnlySpan<ITexture2D> textures)
        {
            GLCoreFrameBuffer<ITexture2D, GLCoreTexture2D> frameBuffer = new GLCoreFrameBuffer<ITexture2D, GLCoreTexture2D>(_device, this, DefaultFrameBufferOption.NotUsed, textures, textures[0].Resolution, textures[0].Layers, false);
            _device.InitializeResource(frameBuffer);
            return frameBuffer;
        }

        #endregion

    }
}
