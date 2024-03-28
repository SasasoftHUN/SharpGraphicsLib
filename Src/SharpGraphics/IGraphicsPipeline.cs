using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using SharpGraphics.Shaders;
using SharpGraphics.Utils;

namespace SharpGraphics
{

    public enum GeometryType : uint
    {
        Points = 0u,

        Lines = 16u,
        LineStrip = 17u,

        Triangles = 32u,
        TriangleStrip = 33u,
        TriangleFan = 34u,

        //No Quads or Poly for OpenGL ¯\_(ツ)_/¯

        //TODO: Implement for Geometry Shader
        //LinesAdjacency = 64u,
        //LineStripAdjacency = 65u,
        //TrianglesAdjacency = 80u,
        //TriangleStripAdjacency = 81u,

        //TODO: Implement for Tessellation Shader
        //Patches = 96u,
    }

    public enum PolygonMode : uint { Fill = 0u, Line = 1u, Point = 2u }
    [Flags] public enum CullMode : uint { None = 0u, Front = 1u, Back = 2u, FrontAndBack = 3u }
    public enum WindingOrder : uint { CounterClockwise = 0u, Clockwise = 1u }
    public readonly struct DepthBias
    {
        public readonly float constantAddFactor;
        public readonly float clamp;
        public readonly float slopeFactor;

        public DepthBias(float constantAddFactor, float clamp, float slopeFactor)
        {
            this.constantAddFactor = constantAddFactor;
            this.clamp = clamp;
            this.slopeFactor = slopeFactor;
        }
    }
    public readonly struct RasterizationOptions
    {
        public readonly bool depthClamp;
        public readonly PolygonMode frontPolygonMode;
        public readonly PolygonMode backPolygonMode;
        public readonly CullMode cullMode;
        public readonly WindingOrder frontFace;

        public readonly DepthBias? depthBias;
        public readonly float lineWidth;

        public RasterizationOptions(PolygonMode polygonMode,
            CullMode cullMode = CullMode.Back, WindingOrder frontFace = WindingOrder.CounterClockwise,
            in DepthBias? depthBias = default(DepthBias?), bool depthClamp = false, float lineWidth = 1f)
        {
            this.depthClamp = depthClamp;
            this.frontPolygonMode = polygonMode;
            this.backPolygonMode = polygonMode;
            this.cullMode = cullMode;
            this.frontFace = frontFace;

            this.depthBias = depthBias;
            this.lineWidth = lineWidth;
        }
        public RasterizationOptions(PolygonMode frontPolygonMode, PolygonMode backPolygonMode,
            CullMode cullMode = CullMode.Back, WindingOrder frontFace = WindingOrder.CounterClockwise,
            in DepthBias? depthBias = default(DepthBias?), bool depthClamp = false, float lineWidth = 1f)
        {
            this.depthClamp = depthClamp;
            this.frontPolygonMode = frontPolygonMode;
            this.backPolygonMode = backPolygonMode;
            this.cullMode = cullMode;
            this.frontFace = frontFace;

            this.depthBias = depthBias;
            this.lineWidth = lineWidth;
        }

        public static RasterizationOptions Default() => new RasterizationOptions(PolygonMode.Fill);
    }

    public enum ComparisonType { Never, Less, Equal, LessOrEqual, Greater, NotEqual, GreaterOrEqual, Always }
    public readonly struct DepthUsage
    {
        public readonly bool testEnabled;
        public readonly ComparisonType comparison;
        public readonly bool write;

        private DepthUsage(bool testEnabled, ComparisonType comparison, bool write)
        {
            this.testEnabled = testEnabled;
            this.comparison = comparison;
            this.write = write;
        }

        public static DepthUsage Disabled(bool write = false) => new DepthUsage(false, ComparisonType.Never, write);
        public static DepthUsage Enabled(ComparisonType comparison = ComparisonType.Less, bool write = true) => new DepthUsage(true, comparison, write);

    }

    public enum BlendFactor : uint
    {
        Zero, One,
        SourceColor, OneMinusSourceColor,
        DestinationColor, OneMinusDestinationColor,
        SourceAlpha, OneMinusSourceAlpha,
        DestinationAlpha, OneMinusDestinationAlpha,
        ConstantColor, OneMinusConstantColor,
        ConstantAlpha, OneMinusConstantAlpha,
        SourceAlphaSaturate,
        DualSourceColor, OneMinusDualSourceColor,
        DualSourceAlpha, OneMinusDualSourceAlpha
    }
    public enum BlendOperation : uint
    {
        Add = 0u, Subtract = 1u, ReverseSubtract = 2u, Min = 3u, Max = 4u,
        //TODO: VK_EXT_blend_operation_advanced
    }
    public readonly struct BlendAttachment
    {
        public readonly BlendFactor sourceColorBlendFactor;
        public readonly BlendFactor destinationColorBlendFactor;
        public readonly BlendOperation colorBlendOperation;
        public readonly BlendFactor sourceAlphaBlendFactor;
        public readonly BlendFactor destinationAlphaBlendFactor;
        public readonly BlendOperation alphaBlendOperation;

        public BlendAttachment(BlendFactor sourceBlendFactor, BlendFactor destinationBlendFactor, BlendOperation operation)
        {
            this.sourceColorBlendFactor = sourceBlendFactor;
            this.destinationColorBlendFactor = destinationBlendFactor;
            this.colorBlendOperation = operation;
            this.sourceAlphaBlendFactor = sourceBlendFactor;
            this.destinationAlphaBlendFactor = destinationBlendFactor;
            this.alphaBlendOperation = operation;
        }
        public BlendAttachment(
            BlendFactor sourceColorBlendFactor, BlendFactor destinationColorBlendFactor, BlendOperation colorOperation,
            BlendFactor sourceAlphaBlendFactor, BlendFactor destinationAlphaBlendFactor, BlendOperation alphaOperation)
        {
            this.sourceColorBlendFactor = sourceColorBlendFactor;
            this.destinationColorBlendFactor = destinationColorBlendFactor;
            this.colorBlendOperation = colorOperation;
            this.sourceAlphaBlendFactor = sourceAlphaBlendFactor;
            this.destinationAlphaBlendFactor = destinationAlphaBlendFactor;
            this.alphaBlendOperation = alphaOperation;
        }

        public static BlendAttachment Incremental() => new BlendAttachment(BlendFactor.One, BlendFactor.Zero, BlendOperation.Add);
        public static BlendAttachment AlphaBlend() => new BlendAttachment(BlendFactor.SourceAlpha, BlendFactor.OneMinusSourceAlpha, BlendOperation.Add, BlendFactor.One, BlendFactor.Zero, BlendOperation.Add);

    }
    [Flags] public enum ColorComponents : uint
    {
        None = 0u,
        Red = 1u, Green = 2u, Blue = 4u, Alpha = 8u,
        RGB = 7u, RGBA = 15u,
    }
    public readonly struct ColorAttachmentUsage
    {
        public readonly BlendAttachment? blend;
        public readonly ColorComponents colorWriteMask;

        public ColorAttachmentUsage(BlendAttachment blend)
        {
            this.blend = blend;
            this.colorWriteMask = ColorComponents.RGBA;
        }
        public ColorAttachmentUsage(ColorComponents colorWriteMask)
        {
            this.blend = default(BlendAttachment?);
            this.colorWriteMask = colorWriteMask;
        }
        public ColorAttachmentUsage(BlendAttachment blend, ColorComponents colorWriteMask)
        {
            this.blend = blend;
            this.colorWriteMask = colorWriteMask;
        }

        public static ColorAttachmentUsage Create() => new ColorAttachmentUsage(ColorComponents.RGBA);

    }

    public readonly ref struct GraphicsPipelineConstuctionParameters
    {

        private readonly IGraphicsShaderProgram[] _shaders;
        private readonly PipelineResourceLayout[]? _resourceLayouts; //Null is valid for optimization (not allocating empty array)
        private readonly ColorAttachmentUsage[]? _colorAttachmentUsages; //Null is valid for optimization (not allocating empty array)

        public readonly GeometryType geometryType;

        public readonly VertexInputs? vertexInputs; //Nullable is valid, no VertexInputs in shader
        public readonly RasterizationOptions? rasterization;
        public readonly DepthUsage depthUsage;

        public readonly IRenderPass renderPass; //Null is INVALID! It is a struct, so it can be constructed with null renderPass. Need to be checked at every usage
        public readonly uint renderPassStep;
        public readonly uint fallbackColorAttachmentUsageIndex; //Which ColorAttachmentUsage should be used if the Graphics API does not support using separate settings for Color Attachments?

        public ReadOnlySpan<IGraphicsShaderProgram> Shaders => _shaders;
        public ReadOnlySpan<PipelineResourceLayout> ResourceLayouts => _resourceLayouts;
        public ReadOnlySpan<ColorAttachmentUsage> ColorAttachmentUsages => _colorAttachmentUsages;

        public GraphicsPipelineConstuctionParameters(
            in ReadOnlySpan<IGraphicsShaderProgram> shaders, GeometryType geometryType, IRenderPass renderPass, //Non-optional parameters
            uint renderPassStep = 0u,
            in VertexInputs? vertexInputs = default(VertexInputs?),
            in ReadOnlySpan<PipelineResourceLayout> resourceLayouts = new ReadOnlySpan<PipelineResourceLayout>(),
            in DepthUsage? depthUsage = default(DepthUsage?),
            in ReadOnlySpan<ColorAttachmentUsage> colorAttachmentUsages = new ReadOnlySpan<ColorAttachmentUsage>(),
            uint fallbackColorAttachmentUsageIndex = 0u)
        {
            _shaders = shaders.ToArray(); //Copy for safety
            _resourceLayouts = resourceLayouts.Length > 0 ? resourceLayouts.ToArray() : null; //Copy for safety
            _colorAttachmentUsages = colorAttachmentUsages.Length > 0 ? colorAttachmentUsages.ToArray() : null; //Copy for safety
            this.fallbackColorAttachmentUsageIndex = fallbackColorAttachmentUsageIndex;

            this.geometryType = geometryType;

            this.vertexInputs = vertexInputs;
            this.rasterization = RasterizationOptions.Default();
            this.depthUsage = depthUsage ?? DepthUsage.Disabled();

            this.renderPass = renderPass;
            this.renderPassStep = renderPassStep;

            AssertGraphicsPipelineConstruction();
        }
        public GraphicsPipelineConstuctionParameters(
            in ReadOnlySpan<IGraphicsShaderProgram> shaders, GeometryType geometryType, IRenderPass renderPass, //Non-optional parameters
            in RasterizationOptions? rasterization, //Non-optional parameters
            uint renderPassStep = 0u,
            in VertexInputs? vertexInputs = default(VertexInputs?),
            in ReadOnlySpan<PipelineResourceLayout> resourceLayouts = new ReadOnlySpan<PipelineResourceLayout>(),
            in DepthUsage? depthUsage = default(DepthUsage?),
            in ReadOnlySpan<ColorAttachmentUsage> colorAttachmentUsages = new ReadOnlySpan<ColorAttachmentUsage>(),
            uint fallbackColorAttachmentUsageIndex = 0u)
        {
            _shaders = shaders.ToArray(); //Copy for safety
            _resourceLayouts = resourceLayouts.Length > 0 ? resourceLayouts.ToArray() : null; //Copy for safety
            _colorAttachmentUsages = colorAttachmentUsages.Length > 0 ? colorAttachmentUsages.ToArray() : null; //Copy for safety
            this.fallbackColorAttachmentUsageIndex = fallbackColorAttachmentUsageIndex;

            this.geometryType = geometryType;

            this.vertexInputs = vertexInputs;
            this.rasterization = rasterization;
            this.depthUsage = depthUsage ?? DepthUsage.Disabled();

            this.renderPass = renderPass;
            this.renderPassStep = renderPassStep;

            AssertGraphicsPipelineConstruction();
        }


        [Conditional("DEBUG")]
        private void AssertGraphicsPipelineConstruction()
        {
            //Assert Shaders
            ReadOnlySpan<IGraphicsShaderProgram> shaders = Shaders;
            Debug.Assert(shaders.Length > 0, "PipelineConstruction has no shaders!");

            bool vertexShaderFound = false;
            bool fragmentShaderFound = false;

            for (int i = 0; i < shaders.Length; i++)
            {
                switch (shaders[i].Stage)
                {
                    case GraphicsShaderStages.Vertex:
                        Debug.Assert(!vertexShaderFound, "Multiple Vertex Shaders specified in PipelineConstruction!");
                        vertexShaderFound = true;
                        break;
                    case GraphicsShaderStages.Fragment:
                        Debug.Assert(!fragmentShaderFound, "Multiple Fragment Shaders specified in PipelineConstruction!");
                        fragmentShaderFound = true;
                        break;
                }
            }

            Debug.Assert(vertexShaderFound, "No Vertex Shader specified in PipelineConstruction!");
            Debug.Assert(fragmentShaderFound, "No Fragment Shader specified in PipelineConstruction!");


            //Assert Vertex Input
            if (vertexInputs.HasValue)
            {
                VertexInputs vertexInputs = this.vertexInputs.Value;
                ReadOnlySpan<VertexInputBinding> vertexBindings = vertexInputs.Bindings;
                ReadOnlySpan<VertexInputAttribute> vertexAttributes = vertexInputs.Attributes;

                for (int i = 0; i < vertexAttributes.Length; i++)
                {
                    Debug.Assert(vertexAttributes[i].binding < vertexBindings.Length, $"PipelineConstruction has a VertexAttribute at index {i} with an incorrect Binding index: {vertexAttributes[i].binding}!");

                    for (int j = i + 1; j < vertexAttributes.Length; j++)
                        Debug.Assert(vertexAttributes[i].location != vertexAttributes[j].location, $"PipelineConstructon has two VertexAttributes (index {i} and {j}) at the some location: {vertexAttributes[i].location}!");
                }
            }


            //TODO: Assert ResourceLayouts
            ReadOnlySpan<PipelineResourceLayout> resourceLayouts = ResourceLayouts;
            if (resourceLayouts.Length > 0)
            {
            }


            //Assert RenderPass
            Debug.Assert(renderPass != null, "PipelineConstruction has no RenderPass"); //it is a struct, so it can be constructed with null renderPass
            if (renderPass != null)
            {
                Debug.Assert(renderPass.Steps.Length > renderPassStep, $"PipelineConstruction references RenderPassStep index {renderPassStep}, but there are only {renderPass.Steps.Length} steps in the RenderPass!");

                ReadOnlySpan<ColorAttachmentUsage> colorAttachmentUsages = ColorAttachmentUsages;
                if (colorAttachmentUsages.Length > 0)
                {
                    int colorAttachmentCount = renderPass.Steps[(int)renderPassStep].ColorAttachmentIndices.Length;
                    Debug.Assert(colorAttachmentUsages.Length == colorAttachmentCount, $"PipelineConstructon has {colorAttachmentUsages.Length} ColorAttachmentUsages, but it's renderPassStep has {colorAttachmentCount} Color Attachments");
                    Debug.Assert(fallbackColorAttachmentUsageIndex < colorAttachmentUsages.Length, $"PipelineConstructon has {colorAttachmentUsages.Length} ColorAttachmentUsages, but it's fallbackColorAttachmentUsageIndex is referencing index {fallbackColorAttachmentUsageIndex}");
                }
            }
        }

    }

    public interface IGraphicsPipeline : IPipeline
    {
    }
}
