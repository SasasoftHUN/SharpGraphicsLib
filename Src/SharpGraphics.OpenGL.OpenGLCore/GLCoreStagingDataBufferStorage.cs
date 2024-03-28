using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.OpenGLCore
{

    internal interface IGLCoreStagingDataBufferStorage : IStagingDataBuffer, IGLCoreDataBuffer
    {
    }
    internal interface IGLCoreStagingDataBufferStorage<T> : IStagingDataBuffer<T>, IGLCoreStagingDataBufferStorage where T : unmanaged
    {
    }

    internal sealed class GLCoreStagingDataBufferStorage<T> : GLCoreDataBuffer<T>, IGLCoreStagingDataBufferStorage<T> where T : unmanaged
    {

        #region Constructors

        internal GLCoreStagingDataBufferStorage(GLCoreGraphicsDevice device, DataBufferType alignmentType, uint dataCapacity) :
            base(device, dataCapacity, DataBufferType.CopySource | DataBufferType.CopyDestination | DataBufferType.Store, alignmentType, MappableMemoryType.DontCare)
        {
        }

        #endregion

    }
}
