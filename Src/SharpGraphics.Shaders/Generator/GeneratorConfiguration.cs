using SharpGraphics.Shaders.Mappings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGraphics.Shaders.Generator
{
    public class GeneratorConfiguration
    {

        #region Properties

        public ShaderLanguageMappings Mappings { get; }
        
        public IEnumerable<string> TargetBackendOverrideNames { get; }
        public bool IsTargetBackendOverriden { get; }

        #endregion

        #region Constructors

        public GeneratorConfiguration(ShaderLanguageMappings mappings)
        {
            Mappings = mappings;
            TargetBackendOverrideNames = new List<string>();
            IsTargetBackendOverriden = false;
        }
        public GeneratorConfiguration(ShaderLanguageMappings mappings, IEnumerable<string> targetBackendOverrideNames)
        {
            Mappings = mappings;
            TargetBackendOverrideNames = targetBackendOverrideNames.Distinct().ToArray();
            IsTargetBackendOverriden = TargetBackendOverrideNames.Any();
        }

        #endregion

    }
}
