using SharpGraphics.OpenGL.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Graphics.OpenGL;

namespace SharpGraphics.OpenGL.OpenGLCore.Commands
{
    internal sealed class GLCoreFlushMappedDeviceMemoryCommand : GLWaitableCommand
    {

        internal GLCoreFlushMappedDeviceMemoryCommand() : base(false) { }

        public override void Execute()
        {
            GL.MemoryBarrier(MemoryBarrierFlags.ClientMappedBufferBarrierBit);
            GL.WaitSync(GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None), WaitSyncFlags.None, 0u); //TODO: Should create Barrier and Fence at the time of writing the data
            base.Execute();
        }

        public override string ToString() => "Flush Mapped Device Memory";

    }
}
