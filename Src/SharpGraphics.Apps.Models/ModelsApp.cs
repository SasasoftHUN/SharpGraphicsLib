using SharpGraphics.GraphicsViews;
using SharpGraphics.Shaders;
using SharpGraphics.Utils;
using SharpGraphics.Utils.OBJ;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using static SharpGraphics.Utils.OBJ.OBJModelGroup;

namespace SharpGraphics.Apps.Models
{

    public class Model
    {
        
        public readonly IDeviceOnlyDataBuffer<Vertex> vertexBuffer;
        public readonly IDeviceOnlyDataBuffer<uint> indexBuffer;
        public readonly int materialIndex;

        public Model(IGraphicsDevice device, GraphicsCommandBuffer copyCommandBuffer, in ReadOnlySpan<Vertex> vertices, in ReadOnlySpan<uint> indices, int materialIndex,
            IStagingDataBuffer<Vertex> vertexStagingBuffer, ref uint vertexStagingOffset, IStagingDataBuffer<uint> indexStagingBuffer, ref uint indexStagingOffset)
        {
            vertexBuffer = device.CreateDeviceOnlyDataBuffer<Vertex>(DataBufferType.VertexData | DataBufferType.Store, vertices.Length);
            vertexBuffer.UseStagingBuffer(vertexStagingBuffer, vertexStagingOffset);
            vertexStagingOffset += (uint)vertices.Length;
            copyCommandBuffer.StoreData(vertexBuffer, vertices);
            vertexBuffer.ReleaseStagingBuffer();

            indexBuffer = device.CreateDeviceOnlyDataBuffer<uint>(DataBufferType.IndexData | DataBufferType.Store, indices.Length);
            indexBuffer.UseStagingBuffer(indexStagingBuffer, indexStagingOffset);
            indexStagingOffset += (uint)indices.Length;
            copyCommandBuffer.StoreData(indexBuffer, indices);
            indexBuffer.ReleaseStagingBuffer();

            this.materialIndex = materialIndex;
        }

    }

    
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
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct MaterialData
    {
        public readonly Vector4 ambientColor;
        public readonly Vector4 diffuseColor;
        public readonly Vector4 specularColor;

        public readonly float specularPower;

        public MaterialData(Vector4 color, float specularPower = 16f, bool blend = false)
        {
            ambientColor = color;
            diffuseColor = color;
            specularColor = color;
            this.specularPower = specularPower;
        }
        public MaterialData(Vector4 ambientColor, Vector4 diffuseColor, Vector4 specularColor, float specularPower, bool blend)
        {
            this.ambientColor = ambientColor;
            this.diffuseColor = diffuseColor;
            this.specularColor = specularColor;
            this.specularPower = specularPower;
        }
        public MaterialData(OBJMaterial objMaterial)
        {
            ambientColor = new Vector4(objMaterial.Ka, 1f);
            diffuseColor = new Vector4(objMaterial.Kd, 1f);
            specularColor = new Vector4(objMaterial.Ks, 1f);
            specularPower = objMaterial.Ns;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct SceneData
    {
        [FieldOffset(0)]
        public Vector3 cameraPosition;
    }

    public class ModelsApp : GraphicsModuleBase
    {

        #region Fields

        private bool _isDisposed;

        private IRenderPass _renderPass;
        private PipelineResourceLayout _scenePipelineResourceLayout;
        private PipelineResourceLayout _instancePipelineResourceLayout;
        private PipelineResourceLayout _materialPipelineResourceLayout;
        private PipelineResourceLayout _skyboxPipelineResourceLayout;
        private IGraphicsPipeline _phongPipeline;
        private IGraphicsPipeline _phongBlendPipeline;
        private IGraphicsPipeline _skyboxPipeline;

        private List<Model> _models;
        private List<Model> _blendModels;

        private FlyingCamera _camera = new FlyingCamera(new Vector3(5f, 1f ,0f), Vector3.Zero, Vector3.UnitY, 3.14f / 4f, 1f, 0.1f, 100f);

        private Transform _transform;
        private IDeviceOnlyDataBuffer<MaterialData> _materialsUBO;
        private TextureSampler _materialSampler;
        private ITexture2D[] _materialDiffuseTextures;
        //private ITexture2D[] _materialNormalTextures;
        private FrameResource<IDeviceOnlyDataBuffer<Transform>> _skyboxTransformUBO;
        private FrameResource<PipelineResource> _skyboxTransformPipelineResource;

        private IDeviceOnlyDataBuffer<Vector3> _skyboxVertexBuffer;
        private IDeviceOnlyDataBuffer<ushort> _skyboxIndexBuffer;
        private TextureSampler _skyboxSampler;
        private ITextureCube _skyboxTexture;
        private PipelineResource _skyboxPipelineResource;

        private FrameResource<IDeviceOnlyDataBuffer<Transform>> _transformUBO;
        private IDeviceOnlyDataBuffer<LightData> _lightDataUBO;
        private FrameResource<IDeviceOnlyDataBuffer<SceneData>> _sceneDataUBO;
        private FrameResource<PipelineResource> _scenePipelineResource;
        private FrameResource<PipelineResource> _instancePipelineResource;
        private PipelineResource[] _materialPipelineResources;

        #endregion

        #region Constructors

        public ModelsApp(in GraphicsModuleContext context) : base(context)
        {
            if (_device.SwapChain == null)
                throw new NullReferenceException("Device SwapChain is null");

            _camera.UserInput = context.view.UserInputSource;

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

            //Create Phong Lighting Pipeline
            _scenePipelineResourceLayout = _device.CreatePipelineResourceLayout(new PipelineResourceProperties(stackalloc PipelineResourceProperty[]
            {
                new PipelineResourceProperty(0u, PipelineResourceType.UniformBuffer, GraphicsShaderStages.Fragment), //LightData
                new PipelineResourceProperty(1u, PipelineResourceType.UniformBuffer, GraphicsShaderStages.Fragment), //SceneData
            }));
            _instancePipelineResourceLayout = _device.CreatePipelineResourceLayout(new PipelineResourceProperties(stackalloc PipelineResourceProperty[]
            {
                new PipelineResourceProperty(0u, 2u, PipelineResourceType.UniformBuffer, GraphicsShaderStages.Vertex), //TransformData
            }));
            _materialPipelineResourceLayout = _device.CreatePipelineResourceLayout(new PipelineResourceProperties(stackalloc PipelineResourceProperty[]
            {
                new PipelineResourceProperty(0u, 3u, PipelineResourceType.UniformBufferDynamic, GraphicsShaderStages.Fragment), //MaterialData
                new PipelineResourceProperty(1u, 4u, PipelineResourceType.CombinedTextureSampler, GraphicsShaderStages.Fragment), //Diffuse Texture
                //new PipelineResourceProperty(2u, 5u, PipelineResourceType.CombinedTextureSampler, GraphicsShaderStages.Fragment), //Normal Texture
            }));

            using (GraphicsShaderPrograms shaderPrograms = _device.CompileShaderPrograms<TransformVertexShader, NormalsFragmentShader>())
            {
                _phongPipeline = _device.CreatePipeline(new GraphicsPipelineConstuctionParameters(
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
                    resourceLayouts: new PipelineResourceLayout[] { _scenePipelineResourceLayout, _instancePipelineResourceLayout, _materialPipelineResourceLayout },
                    depthUsage: DepthUsage.Enabled()
                ));

                _phongBlendPipeline = _device.CreatePipeline(new GraphicsPipelineConstuctionParameters(
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
                    resourceLayouts: new PipelineResourceLayout[] { _scenePipelineResourceLayout, _instancePipelineResourceLayout, _materialPipelineResourceLayout },
                    rasterization: RasterizationOptions.Default(),
                    depthUsage: DepthUsage.Enabled(),
                    colorAttachmentUsages: stackalloc ColorAttachmentUsage[] { new ColorAttachmentUsage(BlendAttachment.AlphaBlend()) }
                ));
            }


            //Create Skybox Pipeline
            _skyboxPipelineResourceLayout = _device.CreatePipelineResourceLayout(new PipelineResourceProperties(stackalloc PipelineResourceProperty[]
            {
                new PipelineResourceProperty(0u, 4u, PipelineResourceType.CombinedTextureSampler, GraphicsShaderStages.Fragment),
            }));

            using (GraphicsShaderPrograms shaderPrograms = _device.CompileShaderPrograms<SkyboxVertexShader, SkyboxFragmentShader>())
                _skyboxPipeline = _device.CreatePipeline(new GraphicsPipelineConstuctionParameters(
                    shaders: shaderPrograms.ShaderPrograms,
                    geometryType: GeometryType.Triangles,
                    renderPass: _renderPass,
                    vertexInputs: new VertexInputs(
                        stackalloc VertexInputBinding[] { VertexInputBinding.Create<Vector3>() },
                        stackalloc VertexInputAttribute[] { new VertexInputAttribute(0u, 0u, DataFormat.RGB32f), }),
                    resourceLayouts: new PipelineResourceLayout[] { _instancePipelineResourceLayout, _skyboxPipelineResourceLayout },
                    depthUsage: DepthUsage.Enabled(ComparisonType.LessOrEqual, false)
                ));


            //Load OBJ model
            OBJModel objModel;
            //using (Stream objStream = ResourceLoader.GetEmbeddedResourceStream("ModelsResources.Suzanne.obj"))
            using (Stream objStream = ResourceLoader.GetEmbeddedResourceStream("ModelsResources.Sponza.sponza.obj"))
            using (StreamReader objStringStream = new StreamReader(objStream))
            using (Stream mtlStream = ResourceLoader.GetEmbeddedResourceStream("ModelsResources.Sponza.sponza.mtl"))
            using (StreamReader mtlStringStream = new StreamReader(mtlStream))
                objModel = OBJImporter.Import(objStringStream, new StreamReader[] { mtlStringStream }, 0.01f);

            //Initialize Models
            {
                using GraphicsCommandBuffer copyCommandBuffer = _device.CommandProcessors[0].CommandBufferFactory.CreateCommandBuffer();
                copyCommandBuffer.Begin();

                //Create Vertex and Index Buffers from Obj groups
                uint vertexCount = 0u;
                uint indexCount = 0u;
                for (int i = 0; i < objModel.Groups.Length; i++)
                {
                    vertexCount += (uint)objModel.Groups[i].Vertices.Length;
                    indexCount += (uint)objModel.Groups[i].Indices.Length;
                }
                using IStagingDataBuffer<Vertex> vertexStagingBuffer = _device.CreateStagingDataBuffer<Vertex>(DataBufferType.VertexData, vertexCount);
                using IStagingDataBuffer<uint> indexStagingBuffer = _device.CreateStagingDataBuffer<uint>(DataBufferType.IndexData, indexCount);
                vertexCount = 0u;
                indexCount = 0u;
                _models = new List<Model>(objModel.Groups.Length);
                _blendModels = new List<Model>(objModel.Groups.Length);
                for (int i = 0; i < objModel.Groups.Length; i++)
                {
                    Model model = new Model(_device, copyCommandBuffer, objModel.Groups[i].Vertices, objModel.Groups[i].Indices,
                        objModel.Groups[i].MaterialIndex, vertexStagingBuffer, ref vertexCount, indexStagingBuffer, ref indexCount);
                    if (objModel.Materials[objModel.Groups[i].MaterialIndex].IsBlended)
                        _blendModels.Add(model);
                    else _models.Add(model);
                }


                //Global Lighting data
                _lightDataUBO = _device.CreateDeviceOnlyDataBuffer<LightData>(DataBufferType.UniformData | DataBufferType.Store, 1u);
                copyCommandBuffer.StoreData(_lightDataUBO, new LightData()
                {
                    direction = Vector3.Normalize(new Vector3(-1f, -1f, 1f)),
                    ambientColor = new Vector4(0.1f, 0.1f, 0.1f, 1f),
                    diffuseColor = new Vector4(0.7f, 0.7f, 0.7f, 1f),
                    specularColor = new Vector4(0.4f, 0.4f, 0.4f, 1f),
                });

                //Skybox Index-Vertex Buffers
                Vector3[] skyboxVertices = new Vector3[]
                {
                    new Vector3(-1, -1, -1), //Front
                    new Vector3(1, -1, -1),
                    new Vector3(1, 1, -1),
                    new Vector3(-1, 1, -1),
                    new Vector3(-1, -1, 1), //Back
                    new Vector3(1, -1, 1),
                    new Vector3(1, 1, 1),
                    new Vector3(-1, 1, 1),
                };
                _skyboxVertexBuffer = _device.CreateDeviceOnlyDataBuffer<Vector3>(DataBufferType.VertexData | DataBufferType.Store, skyboxVertices.Length);
                copyCommandBuffer.StoreData(_skyboxVertexBuffer, skyboxVertices);

                ushort[] skyboxIndices = new ushort[]
                {
                    0, 1, 2, //Back
                    2, 3, 0,
                    4, 6, 5, //Front
                    6, 4, 7,
                    0, 3, 4, //Left
                    4, 3, 7,
                    1, 5, 2, //Right
                    5, 6, 2,
                    1, 0, 4, //Bottom
                    1, 4, 5,
                    3, 2, 6, //Top
                    3, 6, 7,
                };
                _skyboxIndexBuffer = _device.CreateDeviceOnlyDataBuffer<ushort>(DataBufferType.IndexData | DataBufferType.Store, skyboxIndices.Length);
                copyCommandBuffer.StoreData(_skyboxIndexBuffer, skyboxIndices);

                copyCommandBuffer.End();
                copyCommandBuffer.Submit();

                _device.WaitForIdle();

                _skyboxVertexBuffer.ReleaseStagingBuffer();
                _skyboxIndexBuffer.ReleaseStagingBuffer();
            }

            //Initialize Textures
            {
                DataFormat colorFormat = _device.SwapChain.Format.colorFormat.IsSRGB() ? DataFormat.RGBA8srgb : DataFormat.RGBA8un;
                using GraphicsCommandBuffer copyCommandBuffer = _device.CommandProcessors[0].CommandBufferFactory.CreateCommandBuffer();
                copyCommandBuffer.Begin();

                //Create Material UBOs, Textures and PipelineResources for Materials needed for the Obj
                Configuration.Default.PreferContiguousImageBuffers = true;
                MaterialData[] materials = new MaterialData[objModel.Materials.Length];
                _materialPipelineResources = _materialPipelineResourceLayout.CreateResources((uint)objModel.Materials.Length);
                _materialDiffuseTextures = new ITexture2D[objModel.Materials.Length];
                //_materialNormalTextures = new ITexture2D[objModel.Materials.Length];
                using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>($"ModelsResources.IdentityDiffuseTexture.bmp"))
                {
                    _materialDiffuseTextures[0] = _device.CreateTexture2D(colorFormat, new Vector2UInt((uint)loadedImage.Width, (uint)loadedImage.Height), TextureType.Store | TextureType.CopySource | TextureType.ShaderSample, MemoryType.DeviceOnly);
                    if (loadedImage.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels))
                        copyCommandBuffer.StoreTextureData<Rgba32>(_materialDiffuseTextures[0], pixels, TextureLayout.ShaderReadOnly, 0u);
                }
                /*using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>($"ModelsResources.IdentityNormalTexture.bmp"))
                {
                    _materialNormalTextures[0] = _device.CreateTexture2D(DataFormat.RGBA8un, new Vector2UInt((uint)loadedImage.Width, (uint)loadedImage.Height), TextureType.Store | TextureType.CopySource | TextureType.ShaderSample, MemoryType.DeviceOnly);
                    if (loadedImage.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels))
                        copyCommandBuffer.StoreTextureData<Rgba32>(_materialNormalTextures[0], pixels, TextureLayout.ShaderReadOnly, 0u);
                }*/
                //_materialDiffuseTextures[0].GenerateMipmaps(copyCommandBuffer);
                //_materialNormalTextures[0].GenerateMipmaps(copyCommandBuffer);

                uint totalTexelCount = 1536u * 1536u * (uint)materials.Length;
                using IStagingDataBuffer<Rgba32> texelStagingBuffer = _device.CreateStagingDataBuffer<Rgba32>(DataBufferType.Unknown, totalTexelCount);
                uint texelCount = 0u;
                _materialSampler = _device.CreateTextureSampler(new TextureSamplerConstruction(TextureFilterType.Linear, TextureMipMapType.Linear, 16f, TextureWrapType.Repeat));
                _materialsUBO = _device.CreateDeviceOnlyDataBuffer<MaterialData>(DataBufferType.UniformData | DataBufferType.Store, objModel.Materials.Length);
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = new MaterialData(objModel.Materials[i]);
                    _materialPipelineResources[i].BindUniformBufferDynamic(0u, _materialsUBO);

                    if (!string.IsNullOrWhiteSpace(objModel.Materials[i].MapKd))
                    {
                        using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>($"ModelsResources.Sponza.{objModel.Materials[i].MapKd!.Replace('/', '.').Replace('\\', '.')}"))
                        {
                            loadedImage.Mutate(im => im.Flip(FlipMode.Vertical));
                            _materialDiffuseTextures[i] = _device.CreateTexture2D(colorFormat, new Vector2UInt((uint)loadedImage.Width, (uint)loadedImage.Height), TextureType.Store | TextureType.CopySource | TextureType.ShaderSample, MemoryType.DeviceOnly);
                            _materialDiffuseTextures[i].UseStagingBuffer(texelStagingBuffer, texelCount);
                            if (loadedImage.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels))
                                copyCommandBuffer.StoreTextureData<Rgba32>(_materialDiffuseTextures[i], pixels, TextureLayout.ShaderReadOnly, 0u);
                            _materialDiffuseTextures[i].ReleaseStagingBuffers();
                            texelCount += (uint)loadedImage.Width * (uint)loadedImage.Height;
                        }
                        _materialDiffuseTextures[i].GenerateMipmaps(copyCommandBuffer);
                        _materialPipelineResources[i].BindTexture(1u, _materialSampler, _materialDiffuseTextures[i]);
                    }
                    else _materialPipelineResources[i].BindTexture(1u, _materialSampler, _materialDiffuseTextures[0]);

                    /*if (!string.IsNullOrWhiteSpace(objModel.Materials[i].MapDisp))
                    {
                        using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>($"ModelsResources.Sponza.{objModel.Materials[i].MapDisp!.Replace('/', '.').Replace('\\', '.')}"))
                        {
                            loadedImage.Mutate(im => im.Flip(FlipMode.Vertical));
                            _materialNormalTextures[i] = _device.CreateTexture2D(DataFormat.RGBA8un, new Vector2UInt((uint)loadedImage.Width, (uint)loadedImage.Height), TextureType.Store | TextureType.CopySource | TextureType.ShaderSample, MemoryType.DeviceOnly);
                            _materialNormalTextures[i].UseStagingBuffer(texelStagingBuffer, texelCount);
                            if (loadedImage.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels))
                                copyCommandBuffer.StoreTextureData<Rgba32>(_materialNormalTextures[i], pixels, TextureLayout.ShaderReadOnly, 0u);
                            _materialNormalTextures[i].ReleaseStagingBuffers();
                            texelCount += (uint)loadedImage.Width * (uint)loadedImage.Height;
                        }
                        _materialNormalTextures[i].GenerateMipmaps(copyCommandBuffer);
                        _materialPipelineResources[i].BindTexture(2u, _materialSampler, _materialNormalTextures[i]);
                    }
                    else _materialPipelineResources[i].BindTexture(2u, _materialSampler, _materialNormalTextures[0]);*/
                }
                copyCommandBuffer.StoreData(_materialsUBO, materials);

                //Skybox Texture
                _skyboxTexture = _device.CreateTextureCube(colorFormat, new Vector2UInt(1024u), TextureType.Store | TextureType.CopySource | TextureType.ShaderSample, MemoryType.DeviceOnly);
                using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>("ModelsResources.xpos.png"))
                    if (loadedImage.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels))
                        copyCommandBuffer.StoreTextureData<Rgba32>(_skyboxTexture, pixels, TextureLayout.ShaderReadOnly, CubeFace.XPositive, 0u);
                using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>("ModelsResources.xneg.png"))
                    if (loadedImage.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels))
                        copyCommandBuffer.StoreTextureData<Rgba32>(_skyboxTexture, pixels, TextureLayout.ShaderReadOnly, CubeFace.XNegative, 0u);
                using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>("ModelsResources.ypos.png"))
                    if (loadedImage.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels))
                        copyCommandBuffer.StoreTextureData<Rgba32>(_skyboxTexture, pixels, TextureLayout.ShaderReadOnly, CubeFace.YPositive, 0u);
                using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>("ModelsResources.yneg.png"))
                    if (loadedImage.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels))
                        copyCommandBuffer.StoreTextureData<Rgba32>(_skyboxTexture, pixels, TextureLayout.ShaderReadOnly, CubeFace.YNegative, 0u);
                using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>("ModelsResources.zpos.png"))
                    if (loadedImage.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels))
                        copyCommandBuffer.StoreTextureData<Rgba32>(_skyboxTexture, pixels, TextureLayout.ShaderReadOnly, CubeFace.ZPositive, 0u);
                using (Image<Rgba32> loadedImage = ImageSharpLoader.GetEmbeddedResourceImage<Rgba32>("ModelsResources.zneg.png"))
                    if (loadedImage.DangerousTryGetSinglePixelMemory(out Memory<Rgba32> pixels))
                        copyCommandBuffer.StoreTextureData<Rgba32>(_skyboxTexture, pixels, TextureLayout.ShaderReadOnly, CubeFace.ZNegative, 0u);
                _skyboxTexture.GenerateMipmaps(copyCommandBuffer);

                copyCommandBuffer.End();
                copyCommandBuffer.Submit();

                _device.WaitForIdle();

                for (int i = 0; i < _materialDiffuseTextures.Length; i++)
                    if (_materialDiffuseTextures[i] != null)
                        _materialDiffuseTextures[i].ReleaseStagingBuffers();
                /*for (int i = 0; i < _materialNormalTextures.Length; i++)
                    if (_materialNormalTextures[i] != null)
                        _materialNormalTextures[i].ReleaseStagingBuffers();*/
                _skyboxTexture.ReleaseStagingBuffers();
            }


            _transformUBO = new UniversalFrameResource<IDeviceOnlyDataBuffer<Transform>>(_device.SwapChain, () => _device.CreateDeviceOnlyDataBuffer<Transform>(DataBufferType.UniformData | DataBufferType.Store, 1u));
            _sceneDataUBO = new UniversalFrameResource<IDeviceOnlyDataBuffer<SceneData>>(_device.SwapChain, () => _device.CreateDeviceOnlyDataBuffer<SceneData>(DataBufferType.UniformData | DataBufferType.Store, 1u));
            _scenePipelineResource = new BatchCreatedFrameResource<PipelineResource>(_device.SwapChain, (uint count) => _scenePipelineResourceLayout.CreateResources(count));
            _instancePipelineResource = new BatchCreatedFrameResource<PipelineResource>(_device.SwapChain, (uint count) => _instancePipelineResourceLayout.CreateResources(count));

            _skyboxSampler = _device.CreateTextureSampler(new TextureSamplerConstruction(TextureFilterType.Linear, TextureMipMapType.Linear, 16f));
            _skyboxPipelineResource = _skyboxPipelineResourceLayout.CreateResource();
            _skyboxPipelineResource.BindTexture(0u, _skyboxSampler, _skyboxTexture);

            _skyboxTransformUBO = new UniversalFrameResource<IDeviceOnlyDataBuffer<Transform>>(_device.SwapChain, () => _device.CreateDeviceOnlyDataBuffer<Transform>(DataBufferType.UniformData | DataBufferType.Store, 1u));
            _skyboxTransformPipelineResource = new BatchCreatedFrameResource<PipelineResource>(_device.SwapChain, (uint count) => _instancePipelineResourceLayout.CreateResources(count));

            _transform = new Transform(Matrix4x4.Identity, _camera.ViewProjectionMatrix);

            _device.WaitForIdle();
            GC.Collect(2, GCCollectionMode.Forced);
            GC.Collect(2, GCCollectionMode.Forced);
            GC.Collect(2, GCCollectionMode.Forced);
            GC.Collect(2, GCCollectionMode.Forced);
            GC.Collect(2, GCCollectionMode.Forced);
        }

        ~ModelsApp() => Dispose(false);

        #endregion

        #region Graphics Event Handlers

        private void SwapChain_SizeChanged(object? sender, SwapChainSizeChangedEventArgs e)
        {
            _camera.SetProjection(3.14f / 4.0f, e.Size.x / (float)e.Size.y, 0.1f, 100f);
        }

        #endregion

        #region Private Methods



        #endregion

        #region Protected Methods

        protected override void Update()
        {
            float t = _timers.TimeSeconds;

            //_camera.Eye = new Vector3((float)(Math.Cos(t) * 5d), 5f, (float)(Math.Sin(t) * 5d));
            _camera.Update(_timers.DeltaTime);

            _transform = new Transform(Matrix4x4.Identity, _camera.ViewProjectionMatrix);
        }

        protected override void Render()
        {
            if (_device.SwapChain != null && _device.SwapChain.TryBeginFrame(out GraphicsCommandBuffer? commandBuffer, out IFrameBuffer<ITexture2D>? frameBuffer))
            {
                _scenePipelineResource.Resource.BindUniformBuffer(0u, _lightDataUBO);
                _scenePipelineResource.Resource.BindUniformBuffer(1u, _sceneDataUBO);
                commandBuffer.StoreData(_sceneDataUBO.Resource, new SceneData() { cameraPosition = _camera.Eye });

                _instancePipelineResource.Resource.BindUniformBuffer(0u, _transformUBO);
                commandBuffer.StoreData(_transformUBO.Resource, ref _transform);

                _skyboxTransformPipelineResource.Resource.BindUniformBuffer(0u, _skyboxTransformUBO);
                commandBuffer.StoreData(_skyboxTransformUBO.Resource, new Transform(Matrix4x4.CreateTranslation(_camera.Eye), _camera.ViewProjectionMatrix));

                Vector4 clearColor = new Vector4(1f, 0.7f, 0f, 0f) * (MathF.Sin(_timers.TimeSeconds) * 0.5f + 0.5f);

                commandBuffer.BeginRenderPass(_renderPass, frameBuffer, stackalloc Vector4[] { clearColor, new Vector4(1f), });

                //Opaque Models
                commandBuffer.BindPipeline(_phongPipeline);
                commandBuffer.BindResource(0u, _scenePipelineResource);
                foreach (Model model in _models)
                {
                    commandBuffer.BindVertexBuffer(model.vertexBuffer);
                    commandBuffer.BindIndexBuffer(model.indexBuffer);

                    commandBuffer.BindResource(1u, _instancePipelineResource);
                    commandBuffer.BindResource(2u, _materialPipelineResources[model.materialIndex], model.materialIndex);

                    commandBuffer.DrawIndexed(model.indexBuffer.Capacity);
                }

                //Transparent Models
                commandBuffer.BindPipeline(_phongBlendPipeline);
                commandBuffer.BindResource(0u, _scenePipelineResource);
                foreach (Model model in _blendModels)
                {
                    commandBuffer.BindVertexBuffer(model.vertexBuffer);
                    commandBuffer.BindIndexBuffer(model.indexBuffer);

                    commandBuffer.BindResource(1u, _instancePipelineResource);
                    commandBuffer.BindResource(2u, _materialPipelineResources[model.materialIndex], model.materialIndex);

                    commandBuffer.DrawIndexed(model.indexBuffer.Capacity);
                }

                //Skybox
                commandBuffer.BindPipeline(_skyboxPipeline);

                commandBuffer.BindVertexBuffer(_skyboxVertexBuffer);
                commandBuffer.BindIndexBuffer(_skyboxIndexBuffer);

                commandBuffer.BindResource(0u, _skyboxTransformPipelineResource);
                commandBuffer.BindResource(1u, _skyboxPipelineResource);

                commandBuffer.DrawIndexed(_skyboxIndexBuffer.Capacity);

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

                _materialSampler?.Dispose();
                foreach (ITexture2D materialTexture in _materialDiffuseTextures)
                    if (materialTexture != null)
                        materialTexture.Dispose();
                /*foreach (ITexture2D materialTexture in _materialNormalTextures)
                    if (materialTexture != null)
                        materialTexture.Dispose();*/
                foreach (PipelineResource materialResource in _materialPipelineResources)
                    materialResource.Dispose();
                foreach (Model model in _models)
                {
                    model.vertexBuffer.Dispose();
                    model.indexBuffer.Dispose();
                }
                foreach (Model model in _blendModels)
                {
                    model.vertexBuffer.Dispose();
                    model.indexBuffer.Dispose();
                }

                _phongPipeline?.Dispose();
                _phongBlendPipeline?.Dispose();
                _skyboxPipeline.Dispose();
                _renderPass?.Dispose();
                _sceneDataUBO?.Dispose();
                _lightDataUBO?.Dispose();
                _materialsUBO?.Dispose();
                _transformUBO?.Dispose();

                _scenePipelineResource?.Dispose();
                _instancePipelineResource?.Dispose();
                _scenePipelineResourceLayout?.Dispose();
                _skyboxPipelineResource.Dispose();
                _instancePipelineResourceLayout?.Dispose();
                _materialPipelineResourceLayout?.Dispose();
                _skyboxPipelineResourceLayout.Dispose();

                _skyboxTransformPipelineResource.Dispose();
                _skyboxTransformUBO.Dispose();
                _skyboxSampler.Dispose();
                _skyboxTexture?.Dispose();
                _skyboxIndexBuffer.Dispose();
                _skyboxVertexBuffer.Dispose();

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }

        #endregion

        #region Public Methods

        public static ModelsApp Factory(in GraphicsModuleContext context) => new ModelsApp(context);

        #endregion

    }
}