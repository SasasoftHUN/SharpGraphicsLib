using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.OpenGL.Commands
{

    internal sealed class GLResourceInitializationCommand : IGLCommand
    {

        private readonly IGLResource _resource;

        public GLResourceInitializationCommand(IGLResource resource) => _resource = resource;

        public void Execute() => _resource.GLInitialize();

        public override string ToString() => $"Resource Initialization (Resource: <{_resource}>)";

    }

    internal sealed class GLResourceFreeCommand : IGLCommand
    {

        private readonly IGLResource _resource;

        public GLResourceFreeCommand(IGLResource resource) => _resource = resource;

        public void Execute() => _resource.GLFree();

        public override string ToString() => $"Resource Free (Resource: <{_resource}>)";

    }

}
