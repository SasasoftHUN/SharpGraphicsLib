using SharpGraphics.Shaders;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGraphics
{
    public interface IGraphicsDevice : IDevice
    {

        GraphicsSwapChain? SwapChain { get; } //TODO: Remove SwapChain from Device, make it creatable by functions

        IRenderPass CreateRenderPass(in ReadOnlySpan<RenderPassAttachment> attachments, in ReadOnlySpan<RenderPassStep> steps);

        IGraphicsShaderProgram CompileShaderProgram(in GraphicsShaderSource shaderSource);

        IGraphicsPipeline CreatePipeline(in GraphicsPipelineConstuctionParameters constuction);

    }

    public static class GraphicsDeviceExtension
    {

#if NETUNIFIED
        public static GraphicsShaderPrograms CompileShaderPrograms<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TVertex,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TFragment>
            (this IGraphicsDevice device) where TVertex : VertexShaderBase, new() where TFragment : FragmentShaderBase, new()
#else
        public static GraphicsShaderPrograms CompileShaderPrograms<TVertex, TFragment>(this IGraphicsDevice device) where TVertex : VertexShaderBase, new() where TFragment : FragmentShaderBase, new()
#endif
            => new GraphicsShaderPrograms(new IGraphicsShaderProgram[]
                {
                    device.CompileShaderProgram(new GraphicsShaderSource(device.CreateShaderSource<TVertex>(), GraphicsShaderStages.Vertex)),
                    device.CompileShaderProgram(new GraphicsShaderSource(device.CreateShaderSource<TFragment>(), GraphicsShaderStages.Fragment)),
                });

#if NETUNIFIED
        [SkipLocalsInit]
#endif
        public static PipelineResourceLayout CreatePipelineResourceLayoutForInputAttachments(this IGraphicsDevice device, in RenderPassStep step, uint uniqueBindingStart)
        {
#if DEBUG
            if (step.InputAttachmentIndices.Length == 0)
                throw new ArgumentOutOfRangeException("step.inputAttachmentIndices.Length");
#endif

            Span<PipelineResourceProperty> properties = stackalloc PipelineResourceProperty[step.InputAttachmentIndices.Length];
            for (int i = 0; i < properties.Length; i++)
                properties[i] = new PipelineResourceProperty((uint)i, uniqueBindingStart + (uint)i, PipelineResourceType.InputAttachment, GraphicsShaderStages.Fragment);

            return device.CreatePipelineResourceLayout(new PipelineResourceProperties(properties));
        }

    }

}
