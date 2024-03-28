using SharpGraphics.Utils;
using SharpGraphics.Shaders;
using SharpGraphics.GraphicsViews;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace SharpGraphics.Apps.HelloTriangle
{
    public class HelloTriangleApp : GraphicsModuleBase
    {

        #region Fields

        private bool _isDisposed;

        private IRenderPass _renderPass;
        private IGraphicsPipeline _pipeline;

        #endregion

        #region Constructors

        public HelloTriangleApp(in GraphicsModuleContext context) : base(context)
        {
            if (_device.SwapChain == null)
                throw new NullReferenceException("Device SwapChain is null");

            _renderPass = _device.CreateRenderPass(
                    stackalloc RenderPassAttachment[] { new RenderPassAttachment(AttachmentType.Color | AttachmentType.Present, _device.SwapChain.Format.colorFormat) },
                    new RenderPassStep[] { new RenderPassStep(0u) });
            _device.SwapChain.RenderPass = _renderPass;

            IShaderSource vertexShaderSource = _device.CreateShaderSource<HelloVertexShader>();
            IShaderSource fragmentShaderSource = _device.CreateShaderSource<HelloFragmentShader>();

            using IGraphicsShaderProgram vertexShader = _device.CompileShaderProgram(new GraphicsShaderSource(vertexShaderSource, GraphicsShaderStages.Vertex));
            using IGraphicsShaderProgram fragmentShader = _device.CompileShaderProgram(new GraphicsShaderSource(fragmentShaderSource, GraphicsShaderStages.Fragment));

            _pipeline = _device.CreatePipeline(new GraphicsPipelineConstuctionParameters(
            
                shaders: new IGraphicsShaderProgram[] { vertexShader, fragmentShader },
                geometryType: GeometryType.Triangles,
                renderPass: _renderPass
            ));
        }

        ~HelloTriangleApp() => Dispose(false);

        #endregion

        #region Protected Methods

        protected override void Update() { }

        protected override void Render()
        {
            if (_device.SwapChain != null && _device.SwapChain.TryBeginFrame(out GraphicsCommandBuffer? commandBuffer, out IFrameBuffer<ITexture2D>? frameBuffer))
            {
                Vector4 clearColor = new Vector4(1f, 0.7f, 0f, 0f) * (MathF.Sin(_timers.TimeSeconds) * 0.5f + 0.5f);

                commandBuffer.BeginRenderPass(_renderPass, frameBuffer, clearColor);
                commandBuffer.BindPipeline(_pipeline);

                commandBuffer.Draw(3u);

                commandBuffer.EndRenderPass();

                _device.SwapChain.PresentFrame();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                }

                // Free unmanaged resources (unmanaged objects) and override a finalizer below.
                // Set large fields to null.
                Debug.WriteLine("Disposing App from " + (disposing ? "Dispose()" : "Finalizer") + "...");

                _device.WaitForIdle();

                _pipeline.Dispose();
                _renderPass.Dispose();

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Public Methods

        public static HelloTriangleApp Factory(in GraphicsModuleContext context) => new HelloTriangleApp(context);

        #endregion

    }
}
