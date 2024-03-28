using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLCore
{
    //Instantiated only when OpenGL has support for MultiBind Textures
    internal sealed class GLCorePipelineResourceLayout : GLPipelineResourceLayout
    {

        #region Constructors

        internal GLCorePipelineResourceLayout(in PipelineResourceProperties resourceProperties) : base(resourceProperties)
        {
        }

        #endregion

        #region Public Methods

        public override PipelineResource CreateResource() => new GLCorePipelineResource(this);
        public override PipelineResource[] CreateResources(uint count)
        {
            PipelineResource[] result = new PipelineResource[count];
            for (int i = 0; i < count; i++)
                result[i] = new GLCorePipelineResource(this);
            return result;
        }

        #endregion

    }
}
