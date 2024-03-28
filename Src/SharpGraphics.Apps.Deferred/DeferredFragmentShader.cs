using SharpGraphics.Shaders;
using SharpGraphics.Utils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static SharpGraphics.Apps.Deferred.DeferredApp;

namespace SharpGraphics.Apps.Deferred
{
    [FragmentShader]
    public partial class DeferredFragmentShader : FragmentShaderBase
    {

        [Out] public Vector4 fs_out_col;

        [Uniform(Set = 0u, Binding = 0u)][InputAttachmentIndex(Index = 0)] private RenderPassInput<Vector4> positionBuffer;
        [Uniform(Set = 0u, Binding = 1u)][InputAttachmentIndex(Index = 1)] private RenderPassInput<Vector4> diffuseBuffer;
        [Uniform(Set = 0u, Binding = 2u)][InputAttachmentIndex(Index = 2)] private RenderPassInput<Vector4> normalBuffer;

        [Uniform(Set = 1u, Binding = 0u, UniqueBinding = 3u)] private LightData light;
        [Uniform(Set = 1u, Binding = 1u, UniqueBinding = 4u)] private SceneData scene;

        public override void Main()
        {
            Vector4 ambient = light.ambientColor;

            Vector3 normal = normalBuffer.Load().XYZ();
            Vector3 toLight = -light.direction;
            float diffuseIntensity = MathHelper.Clamp(Vector3.Dot(normal, toLight), 0f, 1f);
            Vector4 diffuse = light.diffuseColor * diffuseBuffer.Load() * diffuseIntensity;

            Vector3 position = positionBuffer.Load().XYZ();
            Vector3 reflect = Vector3.Reflect(light.direction, normal);
            Vector3 toCamera = Vector3.Normalize(scene.cameraPosition - position);
            float specularIntensity = MathF.Pow(MathHelper.Clamp(Vector3.Dot(reflect, toCamera), 0f, 1f), 16f);
            Vector4 specular = light.specularColor * specularIntensity;

            fs_out_col = ambient + diffuse;// + specular;
        }

    }
}
