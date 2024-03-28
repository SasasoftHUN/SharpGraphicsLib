using SharpGraphics.Shaders;
using SharpGraphics.GraphicsViews;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SharpGraphics.Apps.SimpleRaytrace
{

    [StructLayout(LayoutKind.Explicit)]
    public struct SceneData
    {
        [FieldOffset(0)]
        public Matrix4x4 viewProjI;
        [FieldOffset(64)]
        public int maxTrace;
        [FieldOffset(68)]
        public int numberOfSpheres;

        public SceneData(int maxTrace)
        {
            this.viewProjI = Matrix4x4.Identity;
            this.maxTrace = maxTrace;
            this.numberOfSpheres = 0;
        }
        public SceneData(in Matrix4x4 viewProjI, int maxTrace, int numberOfSpheres)
        {
            this.viewProjI = viewProjI;
            this.maxTrace = maxTrace;
            this.numberOfSpheres = numberOfSpheres;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct LightData
    {
        [FieldOffset(0)]
        public Vector3 position;
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct SphereData
    {
        [FieldOffset(0)]
        public Vector3 position;
        [FieldOffset(16)]
        public Vector3 color;
        [FieldOffset(28)]
        public float radius;
    }

    public class SimpleRaytraceApp : GraphicsApplicationBase
    {

        #region Fields

        private bool _isDisposed;

        private IRenderPass _renderPass;
        private PipelineResourceLayout _pipelineResourceLayout;
        private IGraphicsPipeline _pipeline;

        private FlyingCamera _camera = new FlyingCamera(new Vector3(0f, 0f, 5f), Vector3.Zero, Vector3.UnitY, 3.14f / 3f, 1f, 1f, 1000f);

        private SphereData[] _spheres = new SphereData[8];
        private LightData _light = new LightData() { position = new Vector3(-5f, 0f, 0f) };
        private SceneData _scene = new SceneData(4);

        private FrameResource<IDeviceOnlyDataBuffer<SphereData>> _sphereDataUBO;
        private FrameResource<IDeviceOnlyDataBuffer<LightData>> _lightDataUBO;
        private FrameResource<IDeviceOnlyDataBuffer<SceneData>> _sceneDataUBO;
        private FrameResource<PipelineResource> _pipelineResource;

        private ITextureCube _skyboxTexture;
        private TextureSampler _sampler;

        #endregion

        #region Constructors

        public SimpleRaytraceApp(GraphicsManagement graphicsManagement, IGraphicsView view) : base(graphicsManagement, view)
        {
            _camera.UserInput = view.UserInputSource;

            _spheres[0] = new SphereData() { color = new Vector3(1, 0, 0), radius = 1 };
            _spheres[1] = new SphereData() { position = new Vector3(6, 0, 0), color = new Vector3(1, 1, 0), radius = 3 };
            _spheres[2] = new SphereData() { position = new Vector3(18, 0, 0), color = new Vector3(0, 1, 1), radius = 0.5f };

            Random r = new Random();
            for (int i = 0; i < 5; i++)
            {
                _spheres[3 + i] = new SphereData()
                {
                    position = new Vector3(10f * (float)Math.Cos(2d * Math.PI * i / 5d), 0, 10f * (float)Math.Sin(2d * Math.PI * i / 5d)),
                    color = new Vector3((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble()),
                    radius = 0.5f + (float)r.NextDouble() * 1.5f
                };
            }
        }

        ~SimpleRaytraceApp() => Dispose(false);

        #endregion

        #region Graphics Event Handlers

        private void SwapChain_SizeChanged(object sender, SwapChainSizeChangedEventArgs e)
        {
            _camera.SetProjection(3.14f / 4.0f, e.Size.x / (float)e.Size.y, 1f, 1000f);
        }

        #endregion

        #region Protected Methods

        protected override void Initialize()
        {
            base.Initialize();

            _graphicsDevice.SwapChain.SizeChanged += SwapChain_SizeChanged;
            SwapChain_SizeChanged(this, new SwapChainSizeChangedEventArgs(_graphicsDevice.SwapChain.Size));

            _renderPass = _graphicsDevice.CreateRenderPass(
                    stackalloc RenderPassAttachment[] { new RenderPassAttachment(AttachmentType.Color | AttachmentType.Present, _graphicsDevice.SwapChain.Format.colorFormat, AttachmentLoadOperation.Undefined, AttachmentStoreOperation.Store) },
                    new RenderPassStep[] { new RenderPassStep(0u) });
            _graphicsDevice.SwapChain.RenderPass = _renderPass;

            _pipelineResourceLayout = _graphicsDevice.CreatePipelineResourceLayout(new PipelineResourceProperties(stackalloc PipelineResourceProperty[]
            {
                new PipelineResourceProperty(0u, PipelineResourceType.UniformBuffer, GraphicsShaderStages.Fragment),
                new PipelineResourceProperty(1u, PipelineResourceType.UniformBuffer, GraphicsShaderStages.Fragment),
                new PipelineResourceProperty(2u, PipelineResourceType.UniformBuffer, GraphicsShaderStages.Fragment),
                new PipelineResourceProperty(3u, PipelineResourceType.CombinedTextureSampler, GraphicsShaderStages.Fragment),
            }));

            using (GraphicsShaderPrograms shaderPrograms = _graphicsDevice.CompileShaderPrograms<RaytraceVertexShader, RaytraceFragmentShader>())
                _pipeline = _graphicsDevice.CreatePipeline(new GraphicsPipelineConstuctionParameters(
                    shaders: shaderPrograms.ShaderPrograms,
                    resourceLayouts: new PipelineResourceLayout[] { _pipelineResourceLayout },
                    geometryType: GeometryType.TriangleStrip,
                    renderPass: _renderPass
                ));


            using GraphicsCommandBuffer copyCommandBuffer = _graphicsDevice.CommandProcessors[0].CommandBufferFactory.CreateCommandBuffer();
            copyCommandBuffer.Begin();

            Configuration.Default.PreferContiguousImageBuffers = true;
            int skyboxLayerPixels = 1024 * 1024;
            Rgba32[] skyboxPixels = new Rgba32[skyboxLayerPixels * 6]; //Testing StoreData for multiple (all) layers at once
            using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>("SimpleRaytraceResources.xpos.png"))
                loadedImage.CopyPixelDataTo(new Span<Rgba32>(skyboxPixels, 0, skyboxLayerPixels));
            using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>("SimpleRaytraceResources.xneg.png"))
                loadedImage.CopyPixelDataTo(new Span<Rgba32>(skyboxPixels, skyboxLayerPixels, skyboxLayerPixels));
            using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>("SimpleRaytraceResources.ypos.png"))
                loadedImage.CopyPixelDataTo(new Span<Rgba32>(skyboxPixels, skyboxLayerPixels * 2, skyboxLayerPixels));
            using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>("SimpleRaytraceResources.yneg.png"))
                loadedImage.CopyPixelDataTo(new Span<Rgba32>(skyboxPixels, skyboxLayerPixels * 3, skyboxLayerPixels));
            using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>("SimpleRaytraceResources.zpos.png"))
                loadedImage.CopyPixelDataTo(new Span<Rgba32>(skyboxPixels, skyboxLayerPixels * 4, skyboxLayerPixels));
            using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>("SimpleRaytraceResources.zneg.png"))
                loadedImage.CopyPixelDataTo(new Span<Rgba32>(skyboxPixels, skyboxLayerPixels * 5, skyboxLayerPixels));

            _skyboxTexture = _graphicsDevice.CreateTextureCube(_graphicsDevice.SwapChain.Format.colorFormat.IsSRGB() ? DataFormat.RGBA8srgb : DataFormat.RGBA8un,
                new Vector2UInt(1024u), TextureType.Store | TextureType.CopySource | TextureType.ShaderSample, MemoryType.DeviceOnly);
            copyCommandBuffer.StoreTextureDataAllFaces<Rgba32>(_skyboxTexture, skyboxPixels, TextureLayout.ShaderReadOnly, 0u);
            _skyboxTexture.GenerateMipmaps(copyCommandBuffer);

            copyCommandBuffer.End();
            copyCommandBuffer.Submit();

            _sphereDataUBO = new UniversalFrameResource<IDeviceOnlyDataBuffer<SphereData>>(_graphicsDevice.SwapChain, () => _graphicsDevice.CreateDeviceOnlyDataBuffer<SphereData>(DataBufferType.UniformData | DataBufferType.Store, _spheres.Length, false));
            _lightDataUBO = new UniversalFrameResource<IDeviceOnlyDataBuffer<LightData>>(_graphicsDevice.SwapChain, () => _graphicsDevice.CreateDeviceOnlyDataBuffer<LightData>(DataBufferType.UniformData | DataBufferType.Store, 1u));
            _sceneDataUBO = new UniversalFrameResource<IDeviceOnlyDataBuffer<SceneData>>(_graphicsDevice.SwapChain, () => _graphicsDevice.CreateDeviceOnlyDataBuffer<SceneData>(DataBufferType.UniformData | DataBufferType.Store, 1u));
            _pipelineResource = new BatchCreatedFrameResource<PipelineResource>(_graphicsDevice.SwapChain, (uint count) => _pipelineResourceLayout.CreateResources(count));
            
            _sampler = _graphicsDevice.CreateTextureSampler(new TextureSamplerConstruction(TextureFilterType.Linear, TextureMipMapType.Linear, 16f));

            _graphicsDevice.WaitForIdle();

            _skyboxTexture.ReleaseStagingBuffers();
        }

        protected override void Update()
        {
            for (int i = 0; i < 5; i++)
            {
                double alpha = 2d * Math.PI / 5d * i + _timers.TimeSeconds / 5d;
                _spheres[3 + i] = new SphereData()
                {
                    position = new Vector3(10f * (float)Math.Cos(alpha), 0, 10f * (float)Math.Sin(alpha)),
                    color = _spheres[3 + i].color,
                    radius = _spheres[3 + i].radius,
                };
            }

            _camera.Update(_timers.DeltaTime);

            Matrix4x4.Invert(_camera.ViewProjectionMatrix, out _scene.viewProjI);
            _scene.numberOfSpheres = _spheres.Length;
        }

        protected override void Render()
        {
            if (_graphicsDevice.SwapChain.TryBeginFrame(out GraphicsCommandBuffer commandBuffer, out IFrameBuffer<ITexture2D> frameBuffer))
            {
                _pipelineResource.Resource.BindUniformBuffer(0u, _sphereDataUBO);
                commandBuffer.StoreData(_sphereDataUBO.Resource, _spheres);
                _pipelineResource.Resource.BindUniformBuffer(1u, _lightDataUBO);
                commandBuffer.StoreData(_lightDataUBO.Resource, ref _light);
                _pipelineResource.Resource.BindUniformBuffer(2u, _sceneDataUBO);
                commandBuffer.StoreData(_sceneDataUBO.Resource, ref _scene);
                _pipelineResource.Resource.BindTexture(3u, _sampler, _skyboxTexture);

                Vector4 clearColor = new Vector4(1f, 0.7f, 0f, 0f) * (MathF.Sin(_timers.TimeSeconds) * 0.5f + 0.5f);

                commandBuffer.BeginRenderPass(_renderPass, frameBuffer, clearColor);
                commandBuffer.BindPipeline(_pipeline);
                commandBuffer.BindResource(0u, _pipelineResource);

                commandBuffer.SetViewportAndScissor(frameBuffer.Resolution);

                commandBuffer.Draw(4u);

                commandBuffer.EndRenderPass();

                _graphicsDevice.SwapChain.PresentFrame();
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
                if (!disposing)
                    Debug.WriteLine($"Disposing App from {(disposing ? "Dispose()" : "Finalizer")}...");

                _graphicsDevice.WaitForIdle();

                _sampler?.Dispose();
                _skyboxTexture?.Dispose();
                _sphereDataUBO?.Dispose();
                _lightDataUBO?.Dispose();
                _sceneDataUBO?.Dispose();
                _pipelineResource.Dispose();
                _pipelineResourceLayout?.Dispose();
                _pipeline.Dispose();
                _renderPass.Dispose();

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

    }
}
