using SharpGraphics.GraphicsViews;
using SharpGraphics.Shaders;
using SharpGraphics.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static SharpGraphics.Utils.ParametricSurfaces;

namespace SharpGraphics.Apps.NormalsThreads
{
    
    [StructLayout(LayoutKind.Sequential)]
    public struct Transform
    {
        public Matrix4x4 mvp;
        public Matrix4x4 world;
        public Matrix4x4 worldIT;

        public Transform(in Matrix4x4 world, in Matrix4x4 vp)
        {
            this.mvp = world * vp;
            this.world = world;
            Matrix4x4.Invert(Matrix4x4.Transpose(world), out this.worldIT);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct LightData
    {
        [FieldOffset(0)]
        public Vector3 direction;

        [FieldOffset(16)]
        public Vector4 ambientColor;
        [FieldOffset(32)]
        public Vector4 diffuseColor;
        [FieldOffset(48)]
        public Vector4 specularColor;

        [FieldOffset(64)]
        public float specularPower;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MaterialData
    {
        public Vector4 ambientColor;
        public Vector4 diffuseColor;
        public Vector4 specularColor;

        public MaterialData(Vector4 color)
        {
            ambientColor = color;
            diffuseColor = color;
            specularColor = color;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct SceneData
    {
        [FieldOffset(0)]
        public Vector3 cameraPosition;
    }


    public class NormalsThreadsApp : GraphicsModuleBase
    {

        private const int SPHERE_ROW_COUNT = 32;
        private const int PARALLELIZATION = 8;

        #region Fields

        private bool _isDisposed;

        private IRenderPass _renderPass;
        private PipelineResourceLayout _pipelineResourceLayout;
        private IGraphicsPipeline _pipeline;

        private IDeviceOnlyDataBuffer<Vertex> _vertexBuffer;
        private IDeviceOnlyDataBuffer<ushort> _indexBuffer;

        private FlyingCamera _camera = new FlyingCamera(new Vector3(5f, 5f ,0f), Vector3.Zero, Vector3.UnitY, 3.14f / 4f, 1f, 1f, 1000f);

        private Transform[] _transforms;
        private MaterialData _material;

        private FrameResource<SecondaryCommandBuffers> _secondaryCommandBuffers;

        private FrameResource<IMappableDataBuffer<Transform>> _transformUBO;
        private IDeviceOnlyDataBuffer<LightData> _lightDataUBO;
        private FrameResource<IMappableDataBuffer<MaterialData>> _materialUBO;
        private FrameResource<IMappableDataBuffer<SceneData>> _sceneDataUBO;
        private FrameResource<PipelineResource[]> _pipelineResource;

        private ITexture2D _texture;
        private TextureSampler _sampler;

        #endregion

        #region Constructors

        public NormalsThreadsApp(in GraphicsModuleContext context) : base(context)
        {
            if (_device.SwapChain == null)
                throw new NullReferenceException("Device SwapChain is null");

            _camera.UserInput = context.view.UserInputSource;
            _camera.SetView(new Vector3(0f, SPHERE_ROW_COUNT, 0f), new Vector3(SPHERE_ROW_COUNT, 1f, SPHERE_ROW_COUNT), Vector3.UnitY);

            _transforms = new Transform[SPHERE_ROW_COUNT * SPHERE_ROW_COUNT];
            _material = new MaterialData(Vector4.One);

            UpdateSpheres();

            _device.SwapChain.SizeChanged += SwapChain_SizeChanged;
            SwapChain_SizeChanged(this, new SwapChainSizeChangedEventArgs(_device.SwapChain.Size));

            _renderPass = _device.CreateRenderPass(
                stackalloc RenderPassAttachment[]
                {
                    new RenderPassAttachment(AttachmentType.Color | AttachmentType.Present, _device.SwapChain.Format.colorFormat),
                    new RenderPassAttachment(AttachmentType.Depth, DataFormat.Depth24un, AttachmentLoadOperation.Clear, AttachmentStoreOperation.Undefined),
                },
                new RenderPassStep[] { new RenderPassStep(0u, 1u) });
            _device.SwapChain.RenderPass = _renderPass;

            _pipelineResourceLayout = _device.CreatePipelineResourceLayout(new PipelineResourceProperties(stackalloc PipelineResourceProperty[]
            {
                new PipelineResourceProperty(0u, PipelineResourceType.UniformBufferDynamic, GraphicsShaderStages.Vertex),
                new PipelineResourceProperty(1u, PipelineResourceType.UniformBuffer, GraphicsShaderStages.Fragment),
                new PipelineResourceProperty(2u, PipelineResourceType.UniformBuffer, GraphicsShaderStages.Fragment),
                new PipelineResourceProperty(3u, PipelineResourceType.UniformBuffer, GraphicsShaderStages.Fragment),
                new PipelineResourceProperty(4u, PipelineResourceType.CombinedTextureSampler, GraphicsShaderStages.Fragment),
            }));

            using (GraphicsShaderPrograms shaderPrograms = _device.CompileShaderPrograms<TransformVertexShader, NormalsFragmentShader>())
                _pipeline = _device.CreatePipeline(new GraphicsPipelineConstuctionParameters(
                    shaders: shaderPrograms.ShaderPrograms,
                    geometryType: GeometryType.Triangles,
                    renderPass: _renderPass,
                    vertexInputs: new VertexInputs(
                        stackalloc VertexInputBinding[] { VertexInputBinding.Create<Vertex>() },
                        stackalloc VertexInputAttribute[]
                        {
                            new VertexInputAttribute(0u, 0u, DataFormat.RGB32f),
                            new VertexInputAttribute(1u, (uint)Marshal.SizeOf<Vector3>(), DataFormat.RGB32f),
                            new VertexInputAttribute(2u, (uint)Marshal.SizeOf<Vector3>() * 2u, DataFormat.RG32f),
                        }),
                    resourceLayouts: new PipelineResourceLayout[] { _pipelineResourceLayout },
                    depthUsage: DepthUsage.Enabled()
                ));


            _secondaryCommandBuffers = new UniversalFrameResource<SecondaryCommandBuffers>(_device.SwapChain, () => new SecondaryCommandBuffers(_device.CommandProcessors[0], PARALLELIZATION));


            GeometryData sphere = ParametricSurfaces.Generate(ParametricSurfaces.Sphere);
            using GraphicsCommandBuffer copyCommandBuffer = _device.CommandProcessors[0].CommandBufferFactory.CreateCommandBuffer();
            copyCommandBuffer.Begin();

            _vertexBuffer = _device.CreateDeviceOnlyDataBuffer<Vertex>(DataBufferType.VertexData | DataBufferType.Store, sphere.vertices.Length);
            copyCommandBuffer.StoreData(_vertexBuffer, sphere.vertices);

            _indexBuffer = _device.CreateDeviceOnlyDataBuffer<ushort>(DataBufferType.IndexData | DataBufferType.Store, sphere.indices.Length);
            copyCommandBuffer.StoreData(_indexBuffer, sphere.indices);

            _lightDataUBO = _device.CreateDeviceOnlyDataBuffer<LightData>(DataBufferType.UniformData | DataBufferType.Store, 1u);
            copyCommandBuffer.StoreData(_lightDataUBO, new LightData()
            {
                direction = Vector3.Normalize(new Vector3(-1f, -1f, 1f)),
                ambientColor = new Vector4(0.1f, 0.1f, 0.1f, 1f),
                diffuseColor = new Vector4(0.7f, 0.7f, 0.7f, 1f),
                specularColor = new Vector4(0.4f, 0.4f, 0.4f, 1f),
                specularPower = 16f,
            });


            Configuration.Default.PreferContiguousImageBuffers = true;
            TextureRange textureMipLevels = new TextureRange(0u, 10u);
            Vector2UInt textureResolution = new Vector2UInt(512u);
            Rgba32[] texturePixels = new Rgba32[GraphicsUtils.CalculateMipLevelsTotalPixelCount(textureMipLevels, textureResolution, 1u)];
            int offset = 0;
            for (uint i = 0u; i < 10u; i++)
            {
                using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>($"NormalsThreadsResources.earth-{i}.png"))
                {
                    int pixelCount = loadedImage.Width * loadedImage.Height;
                    loadedImage.CopyPixelDataTo(new Span<Rgba32>(texturePixels, offset, pixelCount));
                    offset += pixelCount;
                }
            }

            DataFormat colorFormat = _device.SwapChain.Format.colorFormat.IsSRGB() ? DataFormat.RGBA8srgb : DataFormat.RGBA8un;
            _texture = _device.CreateTexture2D(colorFormat, textureResolution, TextureType.Store | TextureType.ShaderSample, MemoryType.DeviceOnly, textureMipLevels.count);
            copyCommandBuffer.StoreTextureData<Rgba32>(_texture, texturePixels, TextureLayout.ShaderReadOnly, textureMipLevels);

            copyCommandBuffer.End();
            copyCommandBuffer.Submit();

            _transformUBO = new UniversalFrameResource<IMappableDataBuffer<Transform>>(_device.SwapChain, () => _device.CreateMappableDataBuffer<Transform>(DataBufferType.UniformData, MappableMemoryType.DeviceLocal));
            _materialUBO = new UniversalFrameResource<IMappableDataBuffer<MaterialData>>(_device.SwapChain, () => _device.CreateMappableDataBuffer<MaterialData>(DataBufferType.UniformData, MappableMemoryType.DeviceLocal, 1u));
            _sceneDataUBO = new UniversalFrameResource<IMappableDataBuffer<SceneData>>(_device.SwapChain, () => _device.CreateMappableDataBuffer<SceneData>(DataBufferType.UniformData | DataBufferType.Store, MappableMemoryType.DeviceLocal, 1u));
            _pipelineResource = new UniversalFrameResource<PipelineResource[]>(_device.SwapChain, () => _pipelineResourceLayout.CreateResources(PARALLELIZATION));

            _sampler = _device.CreateTextureSampler(new TextureSamplerConstruction(TextureFilterType.Linear, TextureMipMapType.Linear, 16f));

            _device.WaitForIdle();
        }

        ~NormalsThreadsApp() => Dispose(false);

        #endregion

        #region Graphics Event Handlers

        private void SwapChain_SizeChanged(object? sender, SwapChainSizeChangedEventArgs e)
        {
            _camera.SetProjection(3.14f / 4.0f, e.Size.x / (float)e.Size.y, 1f, 1000f);
        }

        #endregion

        #region Private Methods

        private void UpdateSpheres()
        {
            Parallel.For(0, SPHERE_ROW_COUNT, (i) =>
            {
                for (int j = 0; j < SPHERE_ROW_COUNT; j++)
                    _transforms[i + j * SPHERE_ROW_COUNT] = new Transform(Matrix4x4.CreateRotationY(_timers.TimeSeconds * (i + j) / MathF.PI * 0.1f) * Matrix4x4.CreateTranslation(new Vector3(i * 2f, 0f, j * 2f)), _camera.ViewProjectionMatrix);
            });
        }

        #endregion

        #region Protected Methods

        protected override void Update()
        {
            float t = _timers.TimeSeconds;

            //_camera.Eye = new Vector3((float)(Math.Cos(t) * 5d), 5f, (float)(Math.Sin(t) * 5d));
            _camera.Update(_timers.DeltaTime);

            UpdateSpheres();
        }

        protected override void Render()
        {
            if (_device.SwapChain != null && _device.SwapChain.TryBeginFrame(out GraphicsCommandBuffer? commandBuffer, out IFrameBuffer<ITexture2D>? frameBuffer))
            {
                _transformUBO.Resource.EnsureCapacity(_transforms.Length);
                IntPtr mappedTransforms = _transformUBO.Resource.MapMemory(); //Test mapping
                mappedTransforms.CopyWithAlignment<Transform>(_transforms, (int)_transformUBO.Resource.ElementOffset);
                _transformUBO.Resource.FlushMappedSystemMemory();
                _transformUBO.Resource.UnMapMemory();
                //commandBuffer.BufferData(_transformUBO.Resource, _transforms);

                IntPtr mappedMaterial = _materialUBO.Resource.MapMemory();
                Marshal.StructureToPtr(_material, mappedMaterial, false);
                _materialUBO.Resource.FlushMappedSystemMemory();
                _materialUBO.Resource.UnMapMemory();
                //commandBuffer.BufferData(_materialUBO.Resource, _material);

                commandBuffer.StoreData(_sceneDataUBO.Resource, new SceneData() { cameraPosition = _camera.Eye });

                Vector4 clearColor = new Vector4(1f, 0.7f, 0f, 0f) * (MathF.Sin(_timers.TimeSeconds) * 0.5f + 0.5f);

                commandBuffer.BeginRenderPass(_renderPass, frameBuffer, stackalloc Vector4[] { clearColor, new Vector4(1f) }, CommandBufferLevel.Secondary);

                Parallel.For(0, PARALLELIZATION, (k) =>
                {
                    GraphicsCommandBuffer secondaryCommandBuffer = _secondaryCommandBuffers.Resource.CommandBuffers[k];
                    PipelineResource pipelineResource = _pipelineResource.Resource[k];
                    secondaryCommandBuffer.Reset();
                    secondaryCommandBuffer.BeginAndContinue(commandBuffer);

                    pipelineResource.BindUniformBufferDynamic(0u, _transformUBO);
                    pipelineResource.BindUniformBuffer(1u, _materialUBO);
                    pipelineResource.BindUniformBuffer(2u, _lightDataUBO);
                    pipelineResource.BindUniformBuffer(3u, _sceneDataUBO);
                    pipelineResource.BindTexture(4u, _sampler, _texture);

                    secondaryCommandBuffer.BindPipeline(_pipeline);

                    secondaryCommandBuffer.BindVertexBuffer(_vertexBuffer);
                    secondaryCommandBuffer.BindIndexBuffer(_indexBuffer);

                    int count = SPHERE_ROW_COUNT / PARALLELIZATION;
                    int n = k == PARALLELIZATION - 1 ? SPHERE_ROW_COUNT : (k + 1) * count;

                    for (int i = k * count; i < n; i++)
                    {
                        for (int j = 0; j < SPHERE_ROW_COUNT; j++)
                        {
                            secondaryCommandBuffer.BindResource(0u, pipelineResource, i + (j * SPHERE_ROW_COUNT));

                            secondaryCommandBuffer.DrawIndexed(_indexBuffer.Capacity);
                        }
                    }

                    secondaryCommandBuffer.End();
                });

                commandBuffer.ExecuteSecondaryCommandBuffers(_secondaryCommandBuffers.Resource.CommandBuffers);

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

                _sampler.Dispose();
                _texture.Dispose();
                _indexBuffer.Dispose();
                _vertexBuffer.Dispose();
                _pipeline.Dispose();
                _renderPass.Dispose();
                _sceneDataUBO.Dispose();
                _lightDataUBO.Dispose();
                _materialUBO.Dispose();
                _transformUBO.Dispose();
                _pipelineResource.Dispose();
                _pipelineResourceLayout.Dispose();
                _secondaryCommandBuffers?.Dispose();

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Public Methods

        public static NormalsThreadsApp Factory(in GraphicsModuleContext context) => new NormalsThreadsApp(context);

        #endregion

    }
}