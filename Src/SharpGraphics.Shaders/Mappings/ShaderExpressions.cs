using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGraphics.Shaders.Mappings
{
    public interface IShaderExpression
    {
        string ExpressionName { get; }
    }

    public class ShaderConstructorExpression : IShaderExpression
    {

        private readonly ShaderType[] _arguments;

        public string ExpressionName { get; }
        public ShaderType Type { get; }
        public ReadOnlySpan<ShaderType> Arguments => _arguments;

        internal ShaderConstructorExpression(ShaderType type, params ShaderType[] arguments)
        {
            if (arguments.Length > 0)
                ExpressionName = $"{type.TypeName}({arguments.Select(a => a.TypeName).Aggregate((a1, a2) => $"{a1},{a2}")})";
            else ExpressionName = $"{type.TypeName}()";
            Type = type;
            _arguments = arguments;
        }

        public bool IsMatching(ShaderType argument) => _arguments.Length == 1 && argument.IsImplicitConvertableTo(_arguments[0]);
        public bool IsMatching(IEnumerable<ShaderType> arguments)
        {
            if (_arguments.Length == arguments.Count())
            {
                int i = 0;
                foreach (ShaderType argument in arguments)
                    if (!argument.IsImplicitConvertableTo(_arguments[i++]))
                        return false;
                return true;
            }
            else return false;
        }
        public override string ToString() => ExpressionName;
    }

    public abstract class ShaderOperatorExpression : IShaderExpression
    {
        public string ExpressionName { get; }
        public string Operator { get; }

        protected ShaderOperatorExpression(string op, string expressionName)
        {
            Operator = op;
            ExpressionName = expressionName;
        }
        public override string ToString() => ExpressionName;
    }
    public class ShaderUnaryOperatorExpression : ShaderOperatorExpression
    {
        public ShaderType Type { get; }
        public bool IsPrefix { get; }
        public bool IsPostfix { get; }

        internal ShaderUnaryOperatorExpression(ShaderType type, string op, bool isPrefix, bool isPostfix) : base(op, $"{op}{type.TypeName}")
        {
            Type = type;
            IsPrefix = isPrefix;
            IsPostfix = isPostfix;
        }
    }
    public class ShaderBinaryOperatorExpression : ShaderOperatorExpression
    {
        public ShaderType Left { get; }
        public ShaderType Right { get; }
        public bool IsAssociative { get; }

        internal ShaderBinaryOperatorExpression(ShaderType left, ShaderType right, string op, bool isAssociative) : base(op, $"{left.TypeName}{op}{right.TypeName}")
        {
            Left = left;
            Right = right;
            IsAssociative = isAssociative;
        }
    }

    public class ShaderMemberAccessExpression : IShaderExpression
    {

        public string ExpressionName { get; }
        public ShaderType Type { get; }
        public ShaderType MemberType { get; }
        public string MemberName { get; }

        internal ShaderMemberAccessExpression(ShaderType type, ShaderType memberType, string memberName)
        {
            ExpressionName = $"{type.TypeName}.{memberName}";
            Type = type;
            MemberType = memberType;
            MemberName = memberName;
        }

        public override string ToString() => ExpressionName;
    }

    public class ShaderFunctionExpression : IShaderExpression
    {

        private readonly ShaderType[] _arguments;

        public string ExpressionName { get; }
        public string FunctionName { get; }
        public ReadOnlySpan<ShaderType> Arguments => _arguments;

        internal ShaderFunctionExpression(string functionName, ShaderType argument) : this(functionName, new ShaderType[] { argument }) { }
        internal ShaderFunctionExpression(string functionName, params ShaderType[] arguments)
        {
            if (arguments.Length > 0)
                ExpressionName = $"{functionName}({arguments.Select(a => a.TypeName).Aggregate((a1, a2) => $"{a1},{a2}")})";
            else ExpressionName = $"{functionName}()";
            FunctionName = functionName;
            _arguments = arguments;
        }

        public bool IsMatching(ShaderType argument) => _arguments.Length == 1 && argument.IsImplicitConvertableTo(_arguments[0]);
        public bool IsMatching(IEnumerable<ShaderType> arguments)
        {
            if (_arguments.Length == arguments.Count())
            {
                int i = 0;
                foreach (ShaderType argument in arguments)
                    if (!argument.IsImplicitConvertableTo(_arguments[i++]))
                        return false;
                return true;
            }
            else return false;
        }
        public override string ToString() => ExpressionName;

    }

    public class ShaderStatementExpression : IShaderExpression
    {
        public string ExpressionName { get; }
        public string StatementName { get; }

        internal ShaderStatementExpression(string statementName)
        {
            ExpressionName = statementName;
            StatementName = statementName;
        }
    }

}
