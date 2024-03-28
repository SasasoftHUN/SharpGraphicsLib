using SharpGraphics.Shaders.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpGraphics.Shaders.StandAloneShaderGenerator.OutputWriters
{
    internal interface IOutputWriter
    {

        void OutShader(GeneratedShader shader);

    }
}
