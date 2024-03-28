using SharpGraphics.Shaders;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Vk = Vulkan;
using VK = Vulkan.Vk;

namespace SharpGraphics.Vulkan
{
    internal sealed class VulkanGraphicsPipeline : VulkanPipeline, IGraphicsPipeline
    {

        #region Fields

        //private bool _isDisposed;

        #endregion

        #region Constructors

        internal VulkanGraphicsPipeline(VulkanGraphicsDevice device, in GraphicsPipelineConstuctionParameters constuction): base(device)
        {
            //_isDisposed = false;

            using RawList<int/*Vk.VkDynamicState*/> dynamicStates = new RawList<int/*Vk.VkDynamicState*/>();

            //Shaders
            ReadOnlySpan<IGraphicsShaderProgram> shaderPrograms = constuction.Shaders;
            Span<Vk.VkPipelineShaderStageCreateInfo> shaderStageCreateInfos = stackalloc Vk.VkPipelineShaderStageCreateInfo[shaderPrograms.Length];
            for (int i = 0; i < shaderPrograms.Length; i++)
                shaderStageCreateInfos[i] = Unsafe.As<VulkanShaderProgram>(shaderPrograms[i]).PipelineShaderStageCreateInfo;

            //Vertex Shader input (attributes)
            Vk.VkPipelineVertexInputStateCreateInfo vertexInputStateCreateInfo =
                new Vk.VkPipelineVertexInputStateCreateInfo()
                {
                    sType = Vk.VkStructureType.PipelineVertexInputStateCreateInfo,
                };

            Span<Vk.VkVertexInputBindingDescription> vertexBindings = stackalloc Vk.VkVertexInputBindingDescription[constuction.vertexInputs.HasValue ? constuction.vertexInputs.Value.Bindings.Length : 0];
            Span<Vk.VkVertexInputAttributeDescription> vertexAttributes = stackalloc Vk.VkVertexInputAttributeDescription[constuction.vertexInputs.HasValue ? constuction.vertexInputs.Value.Attributes.Length : 0];
            if (constuction.vertexInputs.HasValue)
            {
                VertexInputs vertexInputs = constuction.vertexInputs.Value;
                ReadOnlySpan<VertexInputBinding> bindings = vertexInputs.Bindings;
                ReadOnlySpan<VertexInputAttribute> attributes = vertexInputs.Attributes;

                if (bindings.Length > 0 && attributes.Length > 0)
                {
                    vertexInputStateCreateInfo.vertexBindingDescriptionCount = (uint)bindings.Length;
                    for (int i = 0; i < bindings.Length; i++)
                        vertexBindings[i] = bindings[i].ToVkVertexInputBindingDescription((uint)i);
                    vertexInputStateCreateInfo.pVertexBindingDescriptions = UnsafeExtension.AsIntPtr(vertexBindings);

                    vertexInputStateCreateInfo.vertexAttributeDescriptionCount = (uint)attributes.Length;
                    for (int i = 0; i < attributes.Length; i++)
                        vertexAttributes[i] = attributes[i].ToVertexInputAttributeDescription();
                    vertexInputStateCreateInfo.pVertexAttributeDescriptions = UnsafeExtension.AsIntPtr(vertexAttributes);
                }
            }

            //Geometry Type
            Vk.VkPipelineInputAssemblyStateCreateInfo inputAssemblyStateCreateInfo = new Vk.VkPipelineInputAssemblyStateCreateInfo()
            {
                sType = Vk.VkStructureType.PipelineInputAssemblyStateCreateInfo,
                topology = constuction.geometryType.ToVkPrimitiveTopology(),
                primitiveRestartEnable = VK.False,
                /*
                    primitiveRestartEnable controls whether a special vertex index value is treated as restarting the assembly of primitives.
                This enable only applies to indexed draws (vkCmdDrawIndexed, vkCmdDrawMultiIndexedEXT, and vkCmdDrawIndexedIndirect),
                and the special index value is either 0xFFFFFFFF when the indexType parameter of vkCmdBindIndexBuffer is equal to VK_INDEX_TYPE_UINT32, 0xFF
                when indexType is equal to VK_INDEX_TYPE_UINT8_EXT, or 0xFFFF when indexType is equal to VK_INDEX_TYPE_UINT16.
                Primitive restart is not allowed for “list” topologies, unless one of the features primitiveTopologyPatchListRestart (for VK_PRIMITIVE_TOPOLOGY_PATCH_LIST) or primitiveTopologyListRestart (for all other list topologies) is enabled.
                    */
            };

            //Viewport
            Vk.VkPipelineViewportStateCreateInfo viewportStateCreateInfo = new Vk.VkPipelineViewportStateCreateInfo()
            {
                sType = Vk.VkStructureType.PipelineViewportStateCreateInfo,
                viewportCount = 1,
                scissorCount = 1,
            };
            dynamicStates.Add((int)Vk.VkDynamicState.Viewport);
            dynamicStates.Add((int)Vk.VkDynamicState.Scissor);


            //Rasterization
            Vk.VkPipelineRasterizationStateCreateInfo rasterizationStateCreateInfo;
            if (constuction.rasterization.HasValue)
            {
                RasterizationOptions rasterization = constuction.rasterization.Value;
                rasterizationStateCreateInfo = new Vk.VkPipelineRasterizationStateCreateInfo()
                {
                    sType = Vk.VkStructureType.PipelineRasterizationStateCreateInfo,
                    depthClampEnable = rasterization.depthClamp,
                    rasterizerDiscardEnable = Vk.VkBool32.False,

                    polygonMode = rasterization.frontPolygonMode.ToVkPolygonMode(),
                    cullMode = rasterization.cullMode.ToVkCullModeFlags(),
                    frontFace = rasterization.frontFace.ToVkFrontFace(),

                    depthBiasEnable = Vk.VkBool32.False,
                    depthBiasConstantFactor = 0f,
                    depthBiasClamp = 0f,
                    depthBiasSlopeFactor = 0f,

                    lineWidth = rasterization.lineWidth,
                };
                if (rasterization.depthBias.HasValue)
                {
                    DepthBias depthBias = rasterization.depthBias.Value;
                    rasterizationStateCreateInfo.depthBiasEnable = Vk.VkBool32.True;
                    rasterizationStateCreateInfo.depthBiasConstantFactor = depthBias.constantAddFactor;
                    rasterizationStateCreateInfo.depthBiasClamp = depthBias.clamp;
                    rasterizationStateCreateInfo.depthBiasSlopeFactor = depthBias.slopeFactor;
                }
            }
            else rasterizationStateCreateInfo = new Vk.VkPipelineRasterizationStateCreateInfo()
            {
                sType = Vk.VkStructureType.PipelineRasterizationStateCreateInfo,
                depthClampEnable = Vk.VkBool32.False,
                rasterizerDiscardEnable = Vk.VkBool32.True,

                polygonMode = Vk.VkPolygonMode.Fill,
                cullMode = Vk.VkCullModeFlags.Back,
                frontFace = Vk.VkFrontFace.CounterClockwise,

                depthBiasEnable = Vk.VkBool32.False,
                depthBiasConstantFactor = 0f,
                depthBiasClamp = 0f,
                depthBiasSlopeFactor = 0f,

                lineWidth = 1f,
            };

            //Multisampling
            Vk.VkPipelineMultisampleStateCreateInfo multisampleStateCreateInfo = new Vk.VkPipelineMultisampleStateCreateInfo()
            {
                sType = Vk.VkStructureType.PipelineMultisampleStateCreateInfo,
                rasterizationSamples = Vk.VkSampleCountFlags.SampleCount1,
                sampleShadingEnable = Vk.VkBool32.False,
                minSampleShading = 1f,
                alphaToCoverageEnable = Vk.VkBool32.False,
                alphaToOneEnable = Vk.VkBool32.False,
            };

            //Blending
            RenderPassStep renderPassStep = constuction.renderPass.Steps[(int)constuction.renderPassStep];
            ReadOnlySpan<ColorAttachmentUsage> colorAttachmentUsages = constuction.ColorAttachmentUsages;
            ReadOnlySpan<uint> colorAttachmentIndices = renderPassStep.ColorAttachmentIndices;
            Span<Vk.VkPipelineColorBlendAttachmentState> colorBlendAttachmentStates = stackalloc Vk.VkPipelineColorBlendAttachmentState[colorAttachmentIndices.Length];
            if (colorAttachmentUsages.Length == 0)
                for (int i = 0; i < colorAttachmentIndices.Length; i++)
                    colorBlendAttachmentStates[i] = new Vk.VkPipelineColorBlendAttachmentState()
                    {
                        blendEnable = Vk.VkBool32.False,
                        srcColorBlendFactor = Vk.VkBlendFactor.One,
                        dstColorBlendFactor = Vk.VkBlendFactor.Zero,
                        colorBlendOp = Vk.VkBlendOp.Add,
                        srcAlphaBlendFactor = Vk.VkBlendFactor.One,
                        dstAlphaBlendFactor = Vk.VkBlendFactor.Zero,
                        alphaBlendOp = Vk.VkBlendOp.Add,
                        colorWriteMask = Vk.VkColorComponentFlags.R | Vk.VkColorComponentFlags.G | Vk.VkColorComponentFlags.B | Vk.VkColorComponentFlags.A,
                    };
            else for (int i = 0; i < colorAttachmentIndices.Length; i++)
                    colorBlendAttachmentStates[i] = colorAttachmentUsages[i].ToVkPipelineColorBlendAttachmentState();

            Vk.VkPipelineColorBlendStateCreateInfo colorBlendStateCreateInfo = new Vk.VkPipelineColorBlendStateCreateInfo()
            {
                sType = Vk.VkStructureType.PipelineColorBlendStateCreateInfo,
                logicOpEnable = Vk.VkBool32.False,
                logicOp = Vk.VkLogicOp.Copy,

                attachmentCount = (uint)colorBlendAttachmentStates.Length,
                pAttachments = colorBlendAttachmentStates.Length > 0 ? UnsafeExtension.AsIntPtr(colorBlendAttachmentStates) : IntPtr.Zero,
                //blendConstants???
            };

            //Depth-Stencil
            /*Vk.VkPipelineDepthStencilStateCreateInfo depthStencilStateCreateInfo = new Vk.VkPipelineDepthStencilStateCreateInfo()
            {
                sType = Vk.VkStructureType.PipelineDepthStencilStateCreateInfo,
                depthTestEnable = constuction.depthTest.HasValue ? Vk.VkBool32.True : Vk.VkBool32.False,
                depthCompareOp = constuction.depthTest.HasValue ? constuction.depthTest.Value : Vk.VkCompareOp.Never,
                depthWriteEnable = constuction.writeDepth ? Vk.VkBool32.True : Vk.VkBool32.False,
            };*/
            Vk.VkPipelineDepthStencilStateCreateInfo depthStencilStateCreateInfo = new Vk.VkPipelineDepthStencilStateCreateInfo()
            {
                sType = Vk.VkStructureType.PipelineDepthStencilStateCreateInfo,
                depthTestEnable = constuction.depthUsage.testEnabled,
                depthCompareOp = constuction.depthUsage.comparison.ToVkCompareOp(),
                depthWriteEnable = constuction.depthUsage.write,
            };

            //Layout (active textures, uniforms etc.)
            ConstructLayout(_device.Device, constuction, out _layout);


            //Dynamic States
            Vk.VkPipelineDynamicStateCreateInfo dynamicStateCreateInfo = dynamicStates.Count == 0u ? new Vk.VkPipelineDynamicStateCreateInfo() { sType = Vk.VkStructureType.PipelineDynamicStateCreateInfo } :
                new Vk.VkPipelineDynamicStateCreateInfo()
                {
                    sType = Vk.VkStructureType.PipelineDynamicStateCreateInfo,
                    dynamicStateCount = dynamicStates.Count,
                    pDynamicStates = dynamicStates.Pointer,
                };

            //Pipeline Create Info
            VulkanRenderPass vkRenderPass = Unsafe.As<VulkanRenderPass>(constuction.renderPass);

            CreatePipeline(new Vk.VkGraphicsPipelineCreateInfo()
            {
                sType = Vk.VkStructureType.GraphicsPipelineCreateInfo,

                stageCount = (uint)shaderStageCreateInfos.Length,
                pStages = UnsafeExtension.AsIntPtr(shaderStageCreateInfos),

                pVertexInputState = UnsafeExtension.AsIntPtr(ref vertexInputStateCreateInfo),
                pInputAssemblyState = UnsafeExtension.AsIntPtr(ref inputAssemblyStateCreateInfo),
                pTessellationState = IntPtr.Zero,

                pViewportState = UnsafeExtension.AsIntPtr(ref viewportStateCreateInfo),
                pRasterizationState = UnsafeExtension.AsIntPtr(ref rasterizationStateCreateInfo),
                pMultisampleState = UnsafeExtension.AsIntPtr(ref multisampleStateCreateInfo),
                pDepthStencilState = UnsafeExtension.AsIntPtr(ref depthStencilStateCreateInfo),
                pColorBlendState = UnsafeExtension.AsIntPtr(ref colorBlendStateCreateInfo),

                pDynamicState = dynamicStates.Count == 0u ? IntPtr.Zero : UnsafeExtension.AsIntPtr(ref dynamicStateCreateInfo),

                layout = _layout,
                renderPass = vkRenderPass.Pass,
                subpass = constuction.renderPassStep,
                basePipelineHandle = Vk.VkPipeline.Null,
                basePipelineIndex = -1,
            });
        }

        //~VulkanGraphicsPipeline() => Dispose(false);

        #endregion

        #region Protected Methods

        // Protected implementation of Dispose pattern.
        /*protected override void Dispose(bool disposing)
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
                    Debug.WriteLine($"Disposing Vulkan Graphics Pipeline from {(disposing ? "Dispose()" : "Finalizer")}...");

                _isDisposed = true;

                // Call the base class implementation.
                base.Dispose(disposing);
            }
        }*/

        #endregion

    }
}
