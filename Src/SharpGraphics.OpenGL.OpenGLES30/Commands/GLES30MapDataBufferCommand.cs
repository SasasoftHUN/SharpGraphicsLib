using SharpGraphics.OpenGL.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.ES30;

namespace SharpGraphics.OpenGL.OpenGLES30.Commands
{

    internal sealed class GLES30FlushMappedDeviceMemoryCommand : GLWaitableCommand
    {

        internal GLES30FlushMappedDeviceMemoryCommand() : base(false) { }

        public override void Execute()
        {
            //TODO: GLES 3.0 Memory Barrier? GL.MemoryBarrier(MemoryBarrierFlags.ClientMappedBufferBarrierBit);
#if ANDROID
            GL.WaitSync(GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None), 0, 0); //TODO: Should create Barrier and Fence at the time of writing the data
#else
            GL.WaitSync(GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None), WaitSyncFlags.None, 0u); //TODO: Should create Barrier and Fence at the time of writing the data
#endif
            base.Execute();
        }

        public override string ToString() => "Flush Mapped Device Memory";

    }

}
