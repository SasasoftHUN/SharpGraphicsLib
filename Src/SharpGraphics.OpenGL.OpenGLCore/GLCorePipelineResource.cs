using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using SharpGraphics.OpenGL.CommandBuffers;
using SharpGraphics.OpenGL.OpenGLCore.Commands;

namespace SharpGraphics.OpenGL.OpenGLCore
{
    //Instantiated only when OpenGL has support for MultiBind Textures
    internal sealed class GLCorePipelineResource : GLPipelineResource
    {

        #region Constructors

        internal GLCorePipelineResource(GLCorePipelineResourceLayout layout) : base(layout)
        {
        }

        #endregion

        #region Protected Methods

#if NETUNIFIED
        [SkipLocalsInit]
#endif
        protected override void BindResourceTextures(IPipeline pipeline, uint set, GLCommandBufferList glCommandBuffer)
        {
            //base.BindResourceTextures(pipeline, set, glCommandBuffer); //Class instantiated only when OpenGL has support for MultiBind Texture, so it's safe to use

            GLGraphicsPipeline glPipeline = Unsafe.As<GLGraphicsPipeline>(pipeline);
            int textureUnit = glPipeline.ResourceLayoutStartingTextureUnits[(int)set];

            if (_combinedTextureSamplers != null)
            {
                int samplerCount = _combinedTextureSamplers.Length;
                Span<int> bindings = stackalloc int[samplerCount];
                Span<int> textureIDs = stackalloc int[samplerCount];
                Span<int> samplerIDs = stackalloc int[samplerCount];

                for (int i = 0; i < samplerCount; i++)
                {
                    bindings[i] = glPipeline.AreSamplerLocationsExplicit ? (int)_combinedTextureSamplers[i].binding : glPipeline.GetSamplerLocation(_combinedTextureSamplers[i].binding);
                    textureIDs[i] = _combinedTextureSamplers[i].texture.ID;
                    samplerIDs[i] = _combinedTextureSamplers[i].sampler.ID;
                }

                glCommandBuffer.AddCommand(new GLCoreMultiBindCombinedTextureSamplersCommand(bindings, textureUnit, textureIDs, samplerIDs, glCommandBuffer.MemoryAllocator));
                glCommandBuffer.AddResourceStateRestoreCommand(set, new GLCoreMultiUnBindCombinedTextureSamplersCommand(textureUnit, bindings.Length));
                
                textureUnit += samplerCount;
            }

            if (_inputAttachments != null)
            {
                int attachmentCount = _inputAttachments.Length;
                Span<int> bindings = stackalloc int[attachmentCount];
                Span<int> textureIDs = stackalloc int[attachmentCount];

                for (int i = 0; i < attachmentCount; i++)
                {
                    bindings[i] = glPipeline.AreSamplerLocationsExplicit ? (int)_inputAttachments[i].binding : glPipeline.GetSamplerLocation(_inputAttachments[i].binding);
                    textureIDs[i] = _inputAttachments[i].attachment.ID;
                }

                glCommandBuffer.AddCommand(new GLCoreMultiBindTexturesCommand(bindings, textureUnit, textureIDs, glCommandBuffer.MemoryAllocator));
                glCommandBuffer.AddResourceStateRestoreCommand(set, new GLCoreMultiUnBindTexturesCommand(textureUnit, bindings.Length));
            }
        }

        #endregion

    }
}
