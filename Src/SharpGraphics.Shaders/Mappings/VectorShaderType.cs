using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGraphics.Shaders.Mappings
{
    public abstract class VectorShaderType : ShaderType
    {
        public override string TypeName { get; }

        public PrimitiveShaderType ComponentType { get; }
        public abstract uint ComponentCount { get; }

        public ShaderConstructorExpression SingleComponentConstructor { get; }
        public ShaderConstructorExpression AllComponentsConstructor { get; }

        internal VectorShaderType(PrimitiveShaderType componentType)
        {
            TypeName = $"Vector{ComponentCount}{componentType}";

            ComponentType = componentType;

            SingleComponentConstructor = new ShaderConstructorExpression(this, componentType);
            AddConstructor(SingleComponentConstructor);

            PrimitiveShaderType[] allComponents = new PrimitiveShaderType[ComponentCount];
            for (int i = 0; i < ComponentCount; i++)
                allComponents[i] = componentType;
            AllComponentsConstructor = new ShaderConstructorExpression(this, allComponents);
            AddConstructor(AllComponentsConstructor);
        }

        private void AddMemberPermutation(char[] s, int n, int i, ShaderType memberType)
        {
            if (i >= n - 1)
                AddMember(new ShaderMemberAccessExpression(this, memberType, new string(s)));
            else
            {
                AddMemberPermutation(s, n, i + 1, memberType);
                for (int j = i + 1; j < n; j++)
                {
                    char swap = s[i];
                    s[i] = s[j];
                    s[j] = swap;

                    AddMemberPermutation(s, n, i + 1, memberType);

                    swap = s[i];
                    s[i] = s[j];
                    s[j] = swap;
                }
            }
        }
        protected void AddMemberPermutations(string s, ShaderType memberType) => AddMemberPermutation(s.ToArray(), s.Length, 0, memberType);

        private void AddMemberPermutationWithRepeats(string str, char[] data, int last, int index, ShaderType memberType)
        {
            for (int i = 0; i < str.Length; i++)
            {
                data[index] = str[i];

                if (index == last)
                    AddMember(new ShaderMemberAccessExpression(this, memberType, new string(data)));
                else AddMemberPermutationWithRepeats(str, data, last, index + 1, memberType);
            }
        }
        protected void AddMemberPermutationWithRepeats(string s, ShaderType memberType)
        {
            char[] data = new char[s.Length];
            char[] tmp = s.ToArray();

            Array.Sort(tmp);
            string str = new string(tmp);

            AddMemberPermutationWithRepeats(str, data, s.Length - 1, 0, memberType);
        }

    }

    public class Vector2ShaderType : VectorShaderType
    {
        public override uint ComponentCount => 2u;

        internal Vector2ShaderType(PrimitiveShaderType componentType): base(componentType)
        {
            AddMember(new ShaderMemberAccessExpression(this, componentType, "x"));
            AddMember(new ShaderMemberAccessExpression(this, componentType, "y"));

            AddMemberPermutationWithRepeats("xy", this);
        }
    }

    public class Vector3ShaderType : VectorShaderType
    {
        public override uint ComponentCount => 3u;

        internal Vector3ShaderType(PrimitiveShaderType componentType) : base(componentType)
        {
            AddMember(new ShaderMemberAccessExpression(this, componentType, "x"));
            AddMember(new ShaderMemberAccessExpression(this, componentType, "y"));
            AddMember(new ShaderMemberAccessExpression(this, componentType, "z"));

            AddMemberPermutationWithRepeats("xyy", this);
        }

        internal void AddExpressionsFor(Vector2ShaderType vector2Type)
        {
            AddConstructor(new ShaderConstructorExpression(this, new ShaderType[] { vector2Type, ComponentType }));
            AddConstructor(new ShaderConstructorExpression(this, new ShaderType[] { ComponentType, vector2Type }));

            AddMemberPermutationWithRepeats("xy", vector2Type);
            AddMemberPermutationWithRepeats("xz", vector2Type);
            AddMemberPermutationWithRepeats("yz", vector2Type);
        }
    }

    public class Vector4ShaderType : VectorShaderType
    {
        public override uint ComponentCount => 4u;

        internal Vector4ShaderType(PrimitiveShaderType componentType) : base(componentType)
        {
            AddMember(new ShaderMemberAccessExpression(this, componentType, "x"));
            AddMember(new ShaderMemberAccessExpression(this, componentType, "y"));
            AddMember(new ShaderMemberAccessExpression(this, componentType, "z"));
            AddMember(new ShaderMemberAccessExpression(this, componentType, "w"));

            AddMemberPermutationWithRepeats("xyzw", this);
        }

        internal void AddExpressionsFor(Vector2ShaderType vector2Type, Vector3ShaderType vector3Type)
        {
            AddConstructor(new ShaderConstructorExpression(this, new ShaderType[] { vector2Type, ComponentType }));
            AddConstructor(new ShaderConstructorExpression(this, new ShaderType[] { ComponentType, vector2Type }));

            AddConstructor(new ShaderConstructorExpression(this, new ShaderType[] { vector2Type, ComponentType, ComponentType }));
            AddConstructor(new ShaderConstructorExpression(this, new ShaderType[] { ComponentType, vector2Type, ComponentType }));
            AddConstructor(new ShaderConstructorExpression(this, new ShaderType[] { ComponentType, ComponentType, vector2Type }));

            AddConstructor(new ShaderConstructorExpression(this, new ShaderType[] { vector3Type, ComponentType }));
            AddConstructor(new ShaderConstructorExpression(this, new ShaderType[] { ComponentType, vector3Type }));

            AddMemberPermutationWithRepeats("xy", vector2Type);
            AddMemberPermutationWithRepeats("xz", vector2Type);
            AddMemberPermutationWithRepeats("yz", vector2Type);

            AddMemberPermutationWithRepeats("xyz", vector3Type);
            AddMemberPermutationWithRepeats("xyw", vector3Type);
            AddMemberPermutationWithRepeats("yzw", vector3Type);
            AddMemberPermutationWithRepeats("xzw", vector3Type);
        }
    }
}
