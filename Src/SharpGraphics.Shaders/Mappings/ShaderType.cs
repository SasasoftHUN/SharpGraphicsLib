using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGraphics.Shaders.Mappings
{

    public enum PrimitiveShaderTypes
    {
        Void,
        Bool,
        Int, UInt, Float,
    }
    public enum ShaderTypePrecisions : uint
    {
        Bits8 = 8u, Bits16 = 16u, Bits32 = 32u, Bits64 = 64u,
    }

    public abstract class ShaderType
    {

        #region Fields

        private readonly List<ShaderType> _implicitConvertableTo = new List<ShaderType>();
        private readonly Dictionary<string, ShaderConstructorExpression> _constructors = new Dictionary<string, ShaderConstructorExpression>();
        private readonly Dictionary<string, ShaderMemberAccessExpression> _members = new Dictionary<string, ShaderMemberAccessExpression>();

        #endregion

        #region Properties

        public abstract string TypeName { get; }
        public IEnumerable<ShaderType> ImplicitConvertableTo => _implicitConvertableTo;
        public IReadOnlyDictionary<string, ShaderConstructorExpression> Constructors => _constructors;
        public IReadOnlyDictionary<string, ShaderMemberAccessExpression> Members => _members;

        #endregion

        #region Internal Methods

        internal void AddConstructor(ShaderConstructorExpression constructor) => _constructors[constructor.ExpressionName] = constructor;
        internal void AddConstructors(IEnumerable<ShaderConstructorExpression> constructors)
        {
            foreach (ShaderConstructorExpression constructor in constructors)
                AddConstructor(constructor);
        }

        internal void AddMember(ShaderMemberAccessExpression member) => _members[member.MemberName] = member;
        internal void AddMember(IEnumerable<ShaderMemberAccessExpression> members)
        {
            foreach (ShaderMemberAccessExpression member in members)
                AddMember(member);
        }

        internal void AddImplicitConversionTarget(ShaderType type) => _implicitConvertableTo.Add(type);
        internal void AddImplicitConversionTargets(IEnumerable<ShaderType> types)
        {
            foreach (ShaderType type in types)
                AddImplicitConversionTarget(type);
        }

        #endregion

        #region Public Methods

        public bool IsImplicitConvertableTo(ShaderType type) => type == this || _implicitConvertableTo.Contains(type);

        public override string ToString() => TypeName;

        #endregion

    }

    internal sealed class UserStructShaderType : ShaderType
    {
        public override string TypeName { get; }

        public StructDeclarationSyntax Declaration { get; }
        public ITypeSymbol Symbol { get; }

        internal UserStructShaderType(string name, StructDeclarationSyntax declaration, ITypeSymbol symbol)
        {
            TypeName = name;
            Declaration = declaration;
            Symbol = symbol;
        }
    }

}
