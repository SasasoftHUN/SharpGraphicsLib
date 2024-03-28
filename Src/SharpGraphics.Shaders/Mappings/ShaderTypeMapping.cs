using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGraphics.Shaders.Mappings
{

    public interface IShaderExpressionMapping
    {
        IShaderExpression ExpressionMapping { get; }
    }

    public readonly struct ShaderArgumentMapping
    {
        public readonly int mappedIndex;
        public readonly ShaderType type;

        public ShaderArgumentMapping(int mappedIndex, ShaderType type)
        {
            this.mappedIndex = mappedIndex;
            this.type = type;
        }
    }
    public readonly struct ShaderArgumentMappings
    {
        private readonly ShaderArgumentMapping[]? _arguments; //Null is valid for no arguments
        public readonly int invokerMappedToArgumentIndex;

        public ReadOnlySpan<ShaderArgumentMapping> Arguments => _arguments;

        public ShaderArgumentMappings(int invokerMappedToArgumentIndex)
        {
            this.invokerMappedToArgumentIndex = invokerMappedToArgumentIndex;
            _arguments = null;
        }
        public ShaderArgumentMappings(in ShaderArgumentMapping argument)
        {
            invokerMappedToArgumentIndex = -1;
            _arguments = new ShaderArgumentMapping[] { argument };
        }
        public ShaderArgumentMappings(params ShaderArgumentMapping[] arguments)
        {
            invokerMappedToArgumentIndex = -1;
            _arguments = arguments;
        }
        public ShaderArgumentMappings(int invokerMappedToArgumentIndex, in ShaderArgumentMapping argument)
        {
            this.invokerMappedToArgumentIndex = invokerMappedToArgumentIndex;
            _arguments = new ShaderArgumentMapping[] { argument };
        }
        public ShaderArgumentMappings(int invokerMappedToArgumentIndex, params ShaderArgumentMapping[] arguments)
        {
            this.invokerMappedToArgumentIndex = invokerMappedToArgumentIndex;
            _arguments = arguments;
        }
    }

    public class ShaderTypeConstructorMapping : IShaderExpressionMapping
    {

        public IShaderExpression ExpressionMapping { get; }
        public ShaderArgumentMappings Arguments { get; }

        public ShaderTypeConstructorMapping(IShaderExpression expressionMapping)
        {
            ExpressionMapping = expressionMapping;
            Arguments = new ShaderArgumentMappings(-1);
        }
        public ShaderTypeConstructorMapping(IShaderExpression expressionMapping, in ShaderArgumentMappings arguments)
        {
            ExpressionMapping = expressionMapping;
            Arguments = arguments;
        }

        public static ShaderTypeConstructorMapping MatchConstructor(ShaderConstructorExpression shaderConstructor)
        {
            ShaderArgumentMapping[] arguments = new ShaderArgumentMapping[shaderConstructor.Arguments.Length];
            for (int i = 0; i < arguments.Length; i++)
                arguments[i] = new ShaderArgumentMapping(i, shaderConstructor.Arguments[i]);
            return new ShaderTypeConstructorMapping(shaderConstructor, new ShaderArgumentMappings(arguments));
        }

        public override string ToString() => $"({(Arguments.Arguments.Length > 0 ? Arguments.Arguments.ToArray().Select(a => a.type.TypeName).Aggregate((a1, a2) => $"{a1},{a2}") : "")}) Constuctor Mapping";
    }

    public abstract class ShaderTypeOperatorMapping : IShaderExpressionMapping
    {
        public IShaderExpression ExpressionMapping { get; }
        public string Operator { get; }

        protected ShaderTypeOperatorMapping(IShaderExpression expressionMapping, string op)
        {
            ExpressionMapping = expressionMapping;
            Operator = op;
        }
    }
    public class ShaderTypeUnaryOperatorMapping : ShaderTypeOperatorMapping
    {
        public bool IsPrefix { get; }
        public bool IsPostfix { get; }
        public ShaderTypeUnaryOperatorMapping(IShaderExpression expressionMapping, string op, bool isPrefix, bool isPostfix) : base(expressionMapping, op)
        {
            IsPrefix = isPrefix;
            IsPostfix = isPostfix;
        }
        public override string ToString() => $"{Operator} Operator Mapping";
    }
    public class ShaderTypeBinaryOperatorMapping : ShaderTypeOperatorMapping
    {
        public ShaderType Left { get; }
        public ShaderType Right { get; }
        public bool IsAssociative { get; }
        public bool SwapOperands { get; }

        public ShaderTypeBinaryOperatorMapping(IShaderExpression expressionMapping, ShaderType left, ShaderType right, string op, bool swapOperands = false) : base(expressionMapping, op)
        {
            Left = left;
            Right = right;
            IsAssociative = op switch
            {
                "=" => false,
                "??" => false,
                "??=" => false,
                "=>" => false,
                "?:" => false, //Is this really an operator I can override?
                _ => true,
            };
            SwapOperands = swapOperands;
        }
        public override string ToString() => $"{Left.TypeName}{Operator}{Right.TypeName} Operator Mapping";
    }

    public class ShaderTypeMethodMapping : IShaderExpressionMapping
    {
        public IShaderExpression ExpressionMapping { get; }
        public string MethodName { get; }
        public ShaderArgumentMappings Arguments { get; }

        public ShaderTypeMethodMapping(IShaderExpression expressionMapping, string methodName)
        {
            ExpressionMapping = expressionMapping;
            MethodName = methodName;
            Arguments = new ShaderArgumentMappings(-1);
        }
        public ShaderTypeMethodMapping(IShaderExpression expressionMapping, string methodName, in ShaderArgumentMappings arguments)
        {
            ExpressionMapping = expressionMapping;
            MethodName = methodName;
            Arguments = arguments;
        }
        public override string ToString() => $"{MethodName}({(Arguments.Arguments.Length > 0 ? Arguments.Arguments.ToArray().Select(a => a.type.TypeName).Aggregate((a1, a2) => $"{a1},{a2}") : "")}) Method Mapping";
    }

    public class ShaderTypeFieldMapping : IShaderExpressionMapping
    {
        public IShaderExpression ExpressionMapping { get; }
        public string FieldName { get; }

        public ShaderTypeFieldMapping(IShaderExpression expressionMapping, string fieldName)
        {
            ExpressionMapping = expressionMapping;
            FieldName = fieldName;
        }

        public override string ToString() => $"{FieldName} Field Mapping";
    }

    public class ShaderTypeMapping
    {

        private List<ShaderTypeConstructorMapping> _constructorMappings = new List<ShaderTypeConstructorMapping>();
        private Dictionary<string, ShaderTypeUnaryOperatorMapping> _unaryOperatorMappings = new Dictionary<string, ShaderTypeUnaryOperatorMapping>();
        private List<ShaderTypeBinaryOperatorMapping> _binaryOperatorMappings = new List<ShaderTypeBinaryOperatorMapping>();
        private List<ShaderTypeMethodMapping> _methodMappings = new List<ShaderTypeMethodMapping>();
        private Dictionary<string, ShaderTypeFieldMapping> _fieldMappings = new Dictionary<string, ShaderTypeFieldMapping>();

        public ShaderType? TypeMapping { get; }
        public IEnumerable<ShaderTypeConstructorMapping> ConstructorMappings => _constructorMappings;
        public IReadOnlyDictionary<string, ShaderTypeUnaryOperatorMapping> UnaryOperatorMappings => _unaryOperatorMappings;
        public IEnumerable<ShaderTypeBinaryOperatorMapping> BinaryOperatorMappings => _binaryOperatorMappings;
        public IEnumerable<ShaderTypeMethodMapping> MethodMappings => _methodMappings;
        public IReadOnlyDictionary<string, ShaderTypeFieldMapping> FieldMappings => _fieldMappings;

        public ShaderTypeMapping()
        {

        }
        public ShaderTypeMapping(ShaderType typeMapping) => TypeMapping = typeMapping;

        public ShaderTypeMapping MatchConstructors()
        {
            if (TypeMapping != null)
                foreach (ShaderConstructorExpression constructor in TypeMapping.Constructors.Values)
                    _constructorMappings.Add(ShaderTypeConstructorMapping.MatchConstructor(constructor));
            return this;
        }
        public ShaderTypeMapping MatchOperators(IEnumerable<ShaderOperatorExpression> operators)
        {
            if (TypeMapping != null)
                foreach (ShaderOperatorExpression op in operators)
                {
                    switch (op)
                    {
                        case ShaderUnaryOperatorExpression unaryOperatorExpression:
                            if (unaryOperatorExpression.Type == TypeMapping)
                                AddOperatorMapping(new ShaderTypeUnaryOperatorMapping(unaryOperatorExpression, unaryOperatorExpression.Operator, unaryOperatorExpression.IsPrefix, unaryOperatorExpression.IsPostfix));
                            break;

                        case ShaderBinaryOperatorExpression binaryOperatorExpression:
                            if (binaryOperatorExpression.Left == TypeMapping)
                                AddOperatorMapping(new ShaderTypeBinaryOperatorMapping(binaryOperatorExpression, binaryOperatorExpression.Left, binaryOperatorExpression.Right, binaryOperatorExpression.Operator));
                            break;
                    }
                }
                    
            return this;
        }
        public ShaderTypeMapping MatchMembersAsMethods(int memberNameMinLength)
        {
            if (TypeMapping != null)
                foreach (ShaderMemberAccessExpression member in TypeMapping.Members.Values)
                    if (member.MemberName.Length >= memberNameMinLength)
                        AddMethodMapping(new ShaderTypeMethodMapping(member, member.MemberName.ToUpper()));
            return this;
        }
        public ShaderTypeMapping MatchFields(params (string, string)[] fieldNames)
        {
            if (TypeMapping != null)
                foreach ((string, string) fieldName in fieldNames)
                    AddFieldMapping(new ShaderTypeFieldMapping(TypeMapping.Members[fieldName.Item1], fieldName.Item2));
            return this;
        }

        public ShaderTypeMapping AddConstructorMapping(ShaderTypeConstructorMapping constructor)
        {
            _constructorMappings.Add(constructor);
            return this;
        }
        public ShaderTypeMapping AddOperatorMapping(ShaderTypeUnaryOperatorMapping op)
        {
            _unaryOperatorMappings[op.Operator] = op;
            return this;
        }
        public ShaderTypeMapping AddOperatorMapping(ShaderTypeBinaryOperatorMapping op)
        {
            _binaryOperatorMappings.Add(op);
            return this;
        }

        public ShaderTypeMapping AddMethodMapping(ShaderTypeMethodMapping method)
        {
            _methodMappings.Add(method);
            return this;
        }

        public ShaderTypeMapping AddFieldMapping(ShaderTypeFieldMapping field)
        {
            _fieldMappings[field.FieldName] = field;
            return this;
        }

        public override string ToString() => TypeMapping != null ? $"{TypeMapping.TypeName.ToString()} Type Mapping" : "{ShaderTypeMapping}";

    }

}
