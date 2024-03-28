using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGraphics.Shaders.Generator;
using SharpGraphics.Shaders.Mappings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SharpGraphics.Shaders.Builders
{
    internal abstract class TextShaderBuilderBase : ShaderBuilderBase
    {

        #region Fields

        private int _structInsertIndex = 0;
        private int _functionInsertIndex = 0;

        private readonly Dictionary<string, string> _variableNameOverrides = new Dictionary<string, string>();

        protected StringBuilder _sb = new StringBuilder();
        protected readonly StringBuilder _mainSB;
        protected int _indentation = 0;

        protected readonly NumberFormatInfo _floatFormat = new NumberFormatInfo() { NumberDecimalSeparator = "." };

        #endregion

        #region Properties

        public string ShaderSourceCode => _sb.ToString();

        #endregion

        #region Constructors

        protected TextShaderBuilderBase(ShaderLanguageMappings mappings) : base(mappings)
        {
            _mainSB = _sb;
        }

        #endregion

        #region Protected Methods

        protected override string GetVariableName(string varName, TypeSyntax varType)
            => _variableNameOverrides.TryGetValue(varName, out string? varNameOverride) ? varNameOverride : base.GetVariableName(varName, varType);
        protected void AddVariableNameOverride(string name, string nameOverride)
        {
            string key = name;
            foreach (KeyValuePair<string, string> nv in _variableNameOverrides)
                if (nv.Value == name)
                {
                    key = nv.Key;
                    break;
                }
            _variableNameOverrides[key] = nameOverride;
        }

        protected override bool TryBuildNewStruct(ITypeSymbol structSymbol, string structFullName, StructDeclarationSyntax structDeclaration)
        {
            StringBuilder originalBuilder = _sb;
            try
            {
                _sb = new StringBuilder();
                _sb.Append("struct ");
                BuildStructDeclaration(structFullName, structDeclaration);
                _sb.AppendLine(';');

                _mainSB.Insert(_structInsertIndex, _sb.ToString());
                _structInsertIndex += _sb.Length;
                _functionInsertIndex += _sb.Length;
            }
            catch (Exception ex)
            {
                AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3201", $"Unexpected error during Struct Generation ({structSymbol.Name}): {ex.Message.LinesToListing()}", structDeclaration));
                return false;
            }
            finally
            {
                _sb = originalBuilder;
            }

            return true;
        }

        protected override bool TryBuildNewFunction(IMethodSymbol methodSymbol, string name, MethodDeclarationSyntax methodDeclaration)
        {
            StringBuilder originalBuilder = _sb;
            int originalIndentation = _indentation;
            try
            {
                _sb = new StringBuilder();
                _indentation = 0;
                BuildMethod(methodDeclaration, name);

                _mainSB.Insert(_functionInsertIndex, _sb.ToString());
                _functionInsertIndex += _sb.Length;
            }
            catch (Exception ex)
            {
                AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3007", $"Unexpected error during Method Generation ({methodSymbol.Name}): {ex.Message.LinesToListing()}", methodDeclaration.Identifier));
                return false;
            }
            finally
            {
                _sb = originalBuilder;
                _indentation = originalIndentation;
            }
            return true;
        }


        protected override void BuildReadabilityLineSeparator() => _sb.AppendLine();
        protected override void RegisterStructDeclarationInsertIndex() => _structInsertIndex = _sb.Length;
        protected override void RegisterFunctionDeclarationInsertIndex() => _functionInsertIndex = _sb.Length;

        protected void BuildStructDeclaration(string name, StructDeclarationSyntax structDeclaration)
            => BuildStructDeclaration(name, structDeclaration.Members.OfType<FieldDeclarationSyntax>());
        protected void BuildStructDeclaration(string name, IEnumerable<FieldDeclarationSyntax> fields)
            => BuildStructDeclaration(name, fields.Select(f => new ShaderMemberVariableDeclaration(f)));
        protected virtual void BuildStructDeclaration(string name, IEnumerable<ShaderVariableDeclaration> fields)
        {
            _sb.AppendLine(name);
            _sb.AppendLine('{');
            BuildStructDeclarationFields(fields);
            _sb.Append('}');
        }
        protected void BuildStructDeclarationFields(IEnumerable<FieldDeclarationSyntax> fields)
            => BuildStructDeclarationFields(fields.Select(f => new ShaderMemberVariableDeclaration(f)));
        protected virtual void BuildStructDeclarationFields(IEnumerable<ShaderVariableDeclaration> fields)
        {
            foreach (ShaderVariableDeclaration fieldDeclaration in fields)
            {
                if (_cancellationToken.IsCancellationRequested)
                    return;

                if (TryGetTypeName(fieldDeclaration.Field.Declaration.Type, out string? typeName))
                    foreach (VariableDeclaratorSyntax varDec in fieldDeclaration.Field.Declaration.Variables)
                    {
                        _sb.AppendIndentation(1);
                        if (TryBuildVariableDeclaration(fieldDeclaration, fieldDeclaration.Field.Declaration.Type, typeName, GetVariableName(varDec.Identifier.ValueText, fieldDeclaration.Field.Declaration.Type)))
                        {
                            if (varDec.Initializer != null)
                                AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("2200", "Variable cannot be initialized.", varDec.Initializer));
                            _sb.AppendLine(';');
                        }
                    }
                else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3000", $"Unknown Type: {fieldDeclaration.Field.Declaration.Type}", fieldDeclaration.Field.Declaration));
            }
        }


        protected override void BuildVariableDeclaration(ShaderVariableDeclaration variable, TypeSyntax typeSyntax, string type, string name, EqualsValueClauseSyntax? initializer)
        {
            if (TryBuildVariableDeclaration(variable, variable.Field.Declaration.Type, type, name))
            {
                if (initializer != null)
                {
                    _sb.Append(" = ");
                    BuildExpression(initializer.Value);
                }

                _sb.AppendLine(';');
            }
        }
        protected abstract bool TryBuildVariableDeclaration(ShaderVariableDeclaration variable, TypeSyntax typeSyntax, string type, string name);



        protected override void BuildMethodInterface(string type, string name, ParameterListSyntax parameters)
        {
            BuildMethodInterfaceBegin(type, name);
            foreach (ParameterSyntax parameter in parameters.Parameters)
            {
                BuildMethodInterfaceParameter(parameter);
                if (parameter != parameters.Parameters.Last())
                    _sb.Append(", ");
            }
            BuildMethodInterfaceEnd();
        }
        protected virtual void BuildMethodInterfaceBegin(string type, string name)
        {
            _sb.Append(type);
            _sb.Append(' ');
            _sb.Append(name);
            _sb.Append('(');
        }
        protected virtual void BuildMethodInterfaceParameter(ParameterSyntax parameter)
        {
            if (parameter.Type != null)
            {
                if (TryGetTypeName(parameter.Type, out string? type))
                {
                    if (parameter.Modifiers.Count > 0)
                    {
                        if (parameter.Modifiers.Count == 1)
                            BuildMethodInterfaceParameterModifier(parameter.Modifiers[0]);
                        else if (parameter.Modifiers[0].ValueText == "this" && parameter.Modifiers.Count > 1)
                            BuildMethodInterfaceParameterModifier(parameter.Modifiers[1]);
                    }
                    _sb.Append(type);
                    _sb.Append(' ');
                    _sb.Append(parameter.Identifier.ValueText);
                }
                else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3000", $"Unknown Type: {parameter.Type}", parameter));
            }
            else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3000", "Unknown Type.", parameter));
        }
        protected virtual void BuildMethodInterfaceParameterModifier(SyntaxToken syntaxToken)
        {
            switch (syntaxToken.ValueText)
            {
                case "in": _sb.Append("const in "); break;
                case "out": _sb.Append("out "); break;
                case "ref": _sb.Append("inout "); break;
            }
        }
        protected virtual void BuildMethodInterfaceEnd()
        {
            _sb.Append(')');
            _sb.AppendLine();
        }

        protected override void BuildMethodBodyBegin()
        {
            _sb.AppendIndentation('{', _indentation++);
            _sb.AppendLine();
        }
        protected override void BuildMethodBodyEnd()
        {
            _sb.AppendIndentation('}', --_indentation);
            _sb.AppendLine();
        }

        protected override void BuildMethodBodySingleStatementExpression(ExpressionSyntax expression, bool isReturn)
        {
            if (isReturn)
                _sb.AppendIndentation("return ", _indentation);
            else _sb.AppendIndentation(_indentation);
            BuildExpression(expression);
            _sb.AppendLine(';');
        }

        protected override void BuildMethodBodyStatement(StatementSyntax statement)
        {
            _sb.AppendIndentation(_indentation);
            base.BuildMethodBodyStatement(statement);
        }
        protected override void BuildStatement(ExpressionStatementSyntax expression)
        {
            BuildExpression(expression.Expression);
            _sb.AppendLine(';');
        }
        protected override void BuildStatement(LocalDeclarationStatementSyntax localDeclaration)
        {
            if (TryGetTypeName(localDeclaration.Declaration.Type, out string? typeName))
            {
                //TODO: Const?
                foreach (VariableDeclaratorSyntax variableDeclarator in localDeclaration.Declaration.Variables)
                {
                    if (variableDeclarator == localDeclaration.Declaration.Variables.First())
                        BuildLocalDeclarationStatement(localDeclaration.Declaration.Type, typeName, GetVariableName(variableDeclarator.Identifier.ValueText, localDeclaration.Declaration.Type));
                    else BuildLocalDeclarationStatement(localDeclaration.Declaration.Type, GetVariableName(variableDeclarator.Identifier.ValueText, localDeclaration.Declaration.Type));

                    if (variableDeclarator.Initializer != null)
                    {
                        _sb.Append(" = ");
                        BuildExpression(variableDeclarator.Initializer.Value);
                    }

                    if (variableDeclarator != localDeclaration.Declaration.Variables.Last())
                        _sb.Append(", ");
                }
            }
            else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3000", $"Unknown Type: {localDeclaration.Declaration.Type}", localDeclaration.Declaration));
            _sb.AppendLine(';');
        }
        protected override void BuildLocalDeclarationStatement(TypeSyntax type, string typeName, string variableName)
        {
            _sb.Append(typeName);
            _sb.Append(' ');
            _sb.Append(variableName);

            if (type.IsKind(SyntaxKind.ArrayType))
                _sb.Append("[]");
        }
        protected override void BuildLocalDeclarationStatement(TypeSyntax type, string variableName)
        {
            _sb.Append(variableName);

            if (type.IsKind(SyntaxKind.ArrayType))
                _sb.Append("[]");
        }
        protected override void BuildStatement(ReturnStatementSyntax returnStatement)
        {
            if (returnStatement.Expression != null)
            {
                _sb.Append("return ");
                BuildExpression(returnStatement.Expression);
            }
            else _sb.Append("return");
            _sb.AppendLine(';');
        }
        protected override void BuildStatement(BreakStatementSyntax breakStatement) => _sb.AppendLine("break;");
        protected override void BuildStatement(ContinueStatementSyntax continueStatement) => _sb.AppendLine("continue;");
        protected override void BuildStatement(IfStatementSyntax ifStatement)
        {
            _sb.Append("if (");
            BuildExpression(ifStatement.Condition);
            _sb.AppendLine(')');

            if (ifStatement.Statement is BlockSyntax)
                BuildMethodBodyStatement(ifStatement.Statement);
            else
            {
                _indentation++;
                BuildMethodBodyStatement(ifStatement.Statement);
                _indentation--;
            }

            if (ifStatement.Else != null)
            {
                _sb.AppendLineIndentation("else", _indentation);
                if (ifStatement.Else.Statement is BlockSyntax)
                    BuildMethodBodyStatement(ifStatement.Else.Statement);
                else
                {
                    _indentation++;
                    BuildMethodBodyStatement(ifStatement.Else.Statement);
                    _indentation--;
                }
            }
        }
        protected override void BuildStatement(ForStatementSyntax forStatement)
        {
            if (forStatement.Declaration == null || forStatement.Declaration.Variables.Count != 1)
            {
                AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("2500", $"For Statement must have exactly one Variable Declaration: {forStatement.GetText()}", forStatement));
                return;
            }
            if (forStatement.Condition == null)
            {
                AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("2501", $"For Statement must have a Condition: {forStatement.GetText()}", forStatement));
                return;
            }
            if (forStatement.Incrementors.Count != 1)
            {
                AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("2502", $"For Statement must have exactly one Incrementor: {forStatement.GetText()}", forStatement));
                return;
            }

            _sb.Append("for (");

            if (TryGetTypeName(forStatement.Declaration.Type, out string? typeName))
            {
                foreach (VariableDeclaratorSyntax variableDeclarator in forStatement.Declaration.Variables)
                {
                    BuildLocalDeclarationStatement(forStatement.Declaration.Type, typeName, GetVariableName(variableDeclarator.Identifier.ValueText, forStatement.Declaration.Type));
                    if (variableDeclarator.Initializer != null)
                    {
                        _sb.Append(" = ");
                        BuildExpression(variableDeclarator.Initializer.Value);
                    }
                }
            }
            else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3000", $"Unknown Type: {forStatement.Declaration.Type}", forStatement.Declaration));
            _sb.Append("; ");

            BuildExpression(forStatement.Condition);
            _sb.Append("; ");
            BuildExpression(forStatement.Incrementors[0]);
            _sb.AppendLine(')');

            if (forStatement.Statement is BlockSyntax)
                BuildMethodBodyStatement(forStatement.Statement);
            else
            {
                _indentation++;
                BuildMethodBodyStatement(forStatement.Statement);
                _indentation--;
            }
        }
        protected override void BuildStatement(WhileStatementSyntax whileStatement)
        {
            _sb.Append("while (");
            BuildExpression(whileStatement.Condition);
            _sb.AppendLine(')');

            if (whileStatement.Statement is BlockSyntax)
                BuildMethodBodyStatement(whileStatement.Statement);
            else
            {
                _indentation++;
                BuildMethodBodyStatement(whileStatement.Statement);
                _indentation--;
            }
        }
        protected override void BuildStatement(DoStatementSyntax doStatement)
        {
            _sb.AppendLine("do");

            if (doStatement.Statement is BlockSyntax)
                BuildMethodBodyStatement(doStatement.Statement);
            else
            {
                _indentation++;
                BuildMethodBodyStatement(doStatement.Statement);
                _indentation--;
            }

            _sb.AppendIndentation("while (", _indentation);
            BuildExpression(doStatement.Condition);
            _sb.AppendLine(");");
        }
        protected override void BuildStatement(BlockSyntax block)
        {
            _sb.AppendLine('{');
            ++_indentation;

            //Attributes?
            foreach (StatementSyntax statement in block.Statements)
            {
                if (_cancellationToken.IsCancellationRequested)
                    return;
                BuildMethodBodyStatement(statement);
            }

            --_indentation;
            _sb.AppendLineIndentation('}', _indentation);

        }



        protected override void BuildExpression(AssignmentExpressionSyntax assignment)
        {
            BuildExpression(assignment.Left);

            switch (assignment.Kind()) //TODO: Is operator available in ShaderType?
            {
                case SyntaxKind.SimpleAssignmentExpression: _sb.Append(" = "); break;
                case SyntaxKind.AddAssignmentExpression: _sb.Append(" += "); break;
                case SyntaxKind.SubtractAssignmentExpression: _sb.Append(" -= "); break;
                case SyntaxKind.MultiplyAssignmentExpression: _sb.Append(" *= "); break;
                case SyntaxKind.DivideAssignmentExpression: _sb.Append(" /= "); break;
                case SyntaxKind.ModuloAssignmentExpression: _sb.Append(" %= "); break;

                default:
                    _sb.Append(" = ");
                    AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("2006", $"Unsupported Assignment Kind: {assignment.Kind()}", assignment));
                    break;
            }

            BuildExpression(assignment.Right);
        }
        protected override void BuildExpression(ArrayCreationExpressionSyntax arrayCreation)
        {
            //AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("-1", arrayCreation.Initializer.));
            //TODO: Implement support for Nested Arrays (or Convert to Multi-Dimensional if can)
            if (arrayCreation.Type.RankSpecifiers.Count != 1)
            {
                AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3100", $"Nested Arrays not supported: {arrayCreation}", arrayCreation));
                return;
            }

            //TODO: Implement support for Multi-Dimensional Arrays
            if (arrayCreation.Type.RankSpecifiers[0].Sizes.Count != 1)
            {
                AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3101", $"Multi-Dimensional Arrays not supported: {arrayCreation}", arrayCreation));
                return;
            }

            BuildExpression(arrayCreation.Type.RankSpecifiers[0].Sizes[0]);
            BuildArrayInitializationExpression(arrayCreation);
        }
        protected virtual void BuildArrayInitializationExpression(ArrayCreationExpressionSyntax arrayCreation)
        {
            if (TryGetTypeName(arrayCreation.Type.ElementType, out string? elementType))
            {
                _sb.Append(elementType);
                _sb.Append('[');
                _sb.Append(']');

                if (arrayCreation.Initializer != null)
                {
                    _sb.Append(" (");
                    BuildExpression(arrayCreation.Initializer);
                    _sb.Append(')');
                }
            }
            else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3000", $"Unknown Type: {arrayCreation.Type}", arrayCreation));
        }
        protected override void BuildExpression(InitializerExpressionSyntax initializer)
        {
            if (initializer.Expressions.Count > 0)
            {
                BuildExpression(initializer.Expressions[0]);
                for (int i = 1; i < initializer.Expressions.Count; i++)
                {
                    _sb.Append(", ");
                    BuildExpression(initializer.Expressions[i]);
                }
            }
        }

        protected override void BuildBoolLiteral(bool literal) => _sb.Append(literal ? "true" : "false");
        protected override void BuildDoubleLiteral(double literal) => _sb.Append(literal.ToString("0.0#############", _floatFormat));
        protected override void BuildFloatLiteral(float literal) => _sb.Append(literal.ToString("0.0######", _floatFormat));
        protected override void BuildIntLiteral(int literal) => _sb.Append(literal);

        protected override void BuildExpression(ElementAccessExpressionSyntax elementAccess)
        {
            BuildExpression(elementAccess.Expression);

            if (elementAccess.ArgumentList.Arguments.Count > 0)
            {
                _sb.Append('[');
                BuildExpression(elementAccess.ArgumentList.Arguments[0].Expression);
                for (int i = 1; i < elementAccess.ArgumentList.Arguments.Count; i++)
                {
                    _sb.Append(", ");
                    BuildExpression(elementAccess.ArgumentList.Arguments[i].Expression);
                }
                _sb.Append(']');
            }
        }

        protected override void BuildMemberFieldAccessExpression(ExpressionSyntax ownerExpression, string fieldName)
        {
            BuildExpression(ownerExpression);
            _sb.Append('.');
            _sb.Append(fieldName);
        }

        protected override void BuildExpression(CastExpressionSyntax castExpression)
        {
            if (TryGetTypeName(castExpression.Type, out string? typeName))
            {
                _sb.Append(typeName);
                _sb.Append('(');
                BuildExpression(castExpression.Expression);
                _sb.Append(')');
            }
            else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3000", $"Unknown Type: {castExpression.Type}", castExpression));
        }

        protected override void BuildExpression(ParenthesizedExpressionSyntax parenthesized)
        {
            _sb.Append('(');
            BuildExpression(parenthesized.Expression);
            _sb.Append(')');
        }


        protected override void BuildShaderExpression(ExpressionSyntax originalExpression, ShaderConstructorExpression shaderExpression)
        {
            if (TryGetBuiltInTypeName(shaderExpression.Type, out string? typeName))
            {
                _sb.Append(typeName);
                _sb.Append('(');
                if (IsConstructorRemapped(shaderExpression, out ArgumentRemap[]? remap) && remap.Length > 0)
                {
                    if (remap[0].customArgument != null)
                        _sb.Append(remap[0].customArgument);
                    //TODO: else, internal error
                    for (int i = 1; i < remap.Length; i++)
                    {
                        _sb.Append(", ");
                        if (remap[i].customArgument != null)
                            _sb.Append(remap[i].customArgument);
                        //TODO: else, internal error
                    }
                }
                _sb.Append(')');
            }
            else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3661", $"Unknown Shader Type: {originalExpression.GetText().ToString().Trim().LinesToListing()}", originalExpression));
        }
        protected override void BuildShaderExpression(ExpressionSyntax originalExpression, ShaderConstructorExpression shaderExpression, IReadOnlyList<ExpressionSyntax> argumentExpressions)
        {
            if (TryGetBuiltInTypeName(shaderExpression.Type, out string? typeName))
            {
                _sb.Append(typeName);
                _sb.Append('(');
                if (IsConstructorRemapped(shaderExpression, out ArgumentRemap[]? remap))
                {
                    if (remap.Length >= 0)
                    {
                        if (remap[0].mappedIndex >= 0)
                            BuildExpression(argumentExpressions[remap[0].mappedIndex]);
                        else if (remap[0].customArgument != null)
                            _sb.Append(remap[0].customArgument);
                        //TODO: else, internal error
                        for (int i = 1; i < remap.Length; i++)
                        {
                            _sb.Append(", ");
                            if (remap[i].mappedIndex >= 0)
                                BuildExpression(argumentExpressions[remap[i].mappedIndex]);
                            else if (remap[i].customArgument != null)
                                _sb.Append(remap[i].customArgument);
                            //TODO: else, internal error
                        }
                    }
                }
                else
                {
                    if (argumentExpressions.Count > 0)
                    {
                        BuildExpression(argumentExpressions[0]);
                        for (int i = 1; i < argumentExpressions.Count; i++)
                        {
                            _sb.Append(", ");
                            BuildExpression(argumentExpressions[i]);
                        }
                    }
                }
                _sb.Append(')');
            }
            else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3661", $"Unknown Shader Type: {originalExpression.GetText().ToString().Trim().LinesToListing()}", originalExpression));
        }

        protected override void BuildShaderExpression(ExpressionSyntax originalExpression, ShaderUnaryOperatorExpression shaderExpression, ExpressionSyntax expression)
        {
            _sb.Append(shaderExpression.Operator);
            BuildExpression(expression);
        }
        protected override void BuildShaderExpression(ExpressionSyntax originalExpression, ShaderBinaryOperatorExpression shaderExpression, ExpressionSyntax leftExpression, ExpressionSyntax rightExpression)
        {
            BuildExpression(leftExpression);
            _sb.Append(' ');
            _sb.Append(shaderExpression.Operator);
            _sb.Append(' ');
            BuildExpression(rightExpression);
        }

        protected override void BuildShaderExpression(ExpressionSyntax originalExpression, ShaderMemberAccessExpression shaderExpression, ExpressionSyntax invoker)
        {
            _sb.Append('(');
            BuildExpression(invoker);
            _sb.Append(')');
            _sb.Append('.');
            _sb.Append(shaderExpression.MemberName);
        }

        protected override void BuildShaderExpression(ExpressionSyntax originalExpression, ShaderFunctionExpression shaderExpression)
        {
            if (TryGetBuiltInFunctionName(shaderExpression, out string? functionName, out ArgumentRemap[]? remap))
            {
                _sb.Append(functionName);
                _sb.Append('(');
                if (remap != null && remap.Length > 0)
                {
                    if (remap[0].customArgument != null)
                        _sb.Append(remap[0].customArgument);
                    //TODO: else, internal error
                    for (int i = 1; i < remap.Length; i++)
                    {
                        _sb.Append(", ");
                        if (remap[i].customArgument != null)
                            _sb.Append(remap[i].customArgument);
                        //TODO: else, internal error
                    }
                }
                _sb.Append(')');
            }
            else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3631", $"Unknown Shader Function: {originalExpression.GetText().ToString().Trim().LinesToListing()}", originalExpression));
        }
        protected override void BuildShaderExpression(ExpressionSyntax originalExpression, ShaderFunctionExpression shaderExpression, IReadOnlyList<ExpressionSyntax> argumentExpressions)
        {
            if (TryGetBuiltInFunctionName(shaderExpression, out string? functionName, out ArgumentRemap[]? remap))
            {
                _sb.Append(functionName);
                _sb.Append('(');
                if (remap != null)
                {
                    if (remap.Length >= 0)
                    {
                        if (remap[0].mappedIndex >= 0)
                            BuildExpression(argumentExpressions[remap[0].mappedIndex]);
                        else if (remap[0].customArgument != null)
                            _sb.Append(remap[0].customArgument);
                        //TODO: else, internal error
                        for (int i = 1; i < remap.Length; i++)
                        {
                            _sb.Append(", ");
                            if (remap[i].mappedIndex >= 0)
                                BuildExpression(argumentExpressions[remap[i].mappedIndex]);
                            else if (remap[i].customArgument != null)
                                _sb.Append(remap[i].customArgument);
                            //TODO: else, internal error
                        }
                    }
                }
                else
                {
                    if (argumentExpressions.Count > 0)
                    {
                        BuildExpression(argumentExpressions[0]);
                        for (int i = 1; i < argumentExpressions.Count; i++)
                        {
                            _sb.Append(", ");
                            BuildExpression(argumentExpressions[i]);
                        }
                    }
                }
                _sb.Append(')');
            }
            else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3631", $"Unknown Shader Function: {originalExpression.GetText().ToString().Trim().LinesToListing()}", originalExpression));
        }

        protected override void BuildShaderExpression(ExpressionSyntax originalExpression, ShaderStatementExpression shaderExpression)
        {
            if (TryGetBuiltInStatementName(shaderExpression, out string? statementName))
                _sb.Append(statementName);
            else AddDiagnosticsAndFail(ShaderGenerationDiagnositcs.CreateError("3641", $"Unknown Shader Statement: {originalExpression.GetText().ToString().Trim().LinesToListing()}", originalExpression));
        }

        protected override void BuildShaderIdentifierNameExpression(ExpressionSyntax originalExpression, ISymbol symbol)
        {
            if (_variableNameOverrides.TryGetValue(symbol.Name, out string? varName))
                _sb.Append(varName);
            else _sb.Append(symbol.Name);
        }
        protected override void BuildShaderFunctionCallExpression(in GeneratedFunction generatedFunction, IReadOnlyList<ExpressionSyntax> arguments)
        {
            _sb.Append(generatedFunction.name);
            _sb.Append('(');
            if (arguments.Count > 0)
            {
                BuildExpression(arguments[0]);
                for (int i = 1; i < arguments.Count; i++)
                {
                    _sb.Append(", ");
                    BuildExpression(arguments[i]);
                }
            }
            _sb.Append(')');
        }

        #endregion

        #region Public Methods

        //public override IGeneratedShaderSource GetShaderSource() => new GeneratedTextShaderSource(BackendName, IsBuildSuccessful ? _sb.ToString() : "");
        public override IGeneratedShaderSource GetShaderSource() => new GeneratedTextShaderSource(BackendName, _sb.ToString());  //doesn't emit shader string if it's failed to build. Good for release, bad for debug
        public override IGeneratedShaderSource GetShaderSourceEmpty() => new GeneratedTextShaderSource(BackendName, "");

        #endregion

    }
}
