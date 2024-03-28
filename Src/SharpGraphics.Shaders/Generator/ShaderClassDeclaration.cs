using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGraphics.Shaders.Builders;
using SharpGraphics.Shaders.Mappings;
using SharpGraphics.Shaders.Reporters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SharpGraphics.Shaders.Generator
{
    internal abstract class ShaderClassDeclaration
    {

        #region Properties

        public string Name { get; }

        public ClassDeclarationSyntax ClassDeclaration { get; }
        public ITypeSymbol ClassTypeSymbol { get; }

        public IReadOnlyList<ShaderVariableDeclaration> Fields { get; }
        public IReadOnlyList<ShaderInVariableDeclaration> InVars { get;}
        public IReadOnlyList<ShaderOutVariableDeclaration> OutVars { get;}
        public IReadOnlyList<ShaderUniformVariableDeclaration> Uniforms { get; }
        public IReadOnlyList<ShaderLocalVariableDeclaration> Locals { get; }

        public IDictionary<string, ShaderVariableDeclaration> StageInputs { get; }
        public IDictionary<string, ShaderVariableDeclaration> StageOutputs { get; }

        public IReadOnlyList<MethodDeclarationSyntax> Methods { get; }
        public MethodDeclarationSyntax EntryMethod { get; }

        public Compilation Compilation { get; }
        public SemanticModel Model { get; }

        public CancellationToken CancellationToken { get; }
        public bool IsValidForGeneration { get; }
        public IEnumerable<string> TargetBackendNames { get; }

        #endregion

        #region Constructors

        public ShaderClassDeclaration(ClassDeclarationSyntax classDeclaration, ShaderLanguageMappings mappings, IDiagnosticReporter diagnosticReporter, Compilation compilation, SemanticModel model, CancellationToken cancellationToken)
        {
            Compilation = compilation;
            Model = model;
            CancellationToken = cancellationToken;

            IsValidForGeneration = true;
            Name = classDeclaration.Identifier.ValueText;
            ClassDeclaration = classDeclaration;

            ITypeSymbol? typeSymbol = model.GetDeclaredSymbol(classDeclaration, cancellationToken) as ITypeSymbol;
            if (typeSymbol != null)
                ClassTypeSymbol = typeSymbol;
            else
            {
                typeSymbol = model.GetTypeInfo(classDeclaration, cancellationToken).Type;
                if (typeSymbol != null)
                    ClassTypeSymbol = typeSymbol;
                else
                {
                    SymbolInfo typeSymbolInfo = model.GetSymbolInfo(classDeclaration, cancellationToken);
                    if (typeSymbolInfo.Symbol != null && typeSymbolInfo.Symbol is ITypeSymbol typeSymbolFallback)
                        ClassTypeSymbol = typeSymbolFallback;
                    else
                    {
                        ClassTypeSymbol = null!;
                        diagnosticReporter.ReportError("1400", $"Failed to get Shader Class Type Info: {classDeclaration.Identifier.Text}.", classDeclaration.Identifier.GetLocation());
                        IsValidForGeneration = false;
                    }
                }
            }

            List<ShaderVariableDeclaration> fields = new List<ShaderVariableDeclaration>();
            List<ShaderInVariableDeclaration> inVars = new List<ShaderInVariableDeclaration>();
            List<ShaderOutVariableDeclaration> outVars = new List<ShaderOutVariableDeclaration>();
            List<ShaderUniformVariableDeclaration> uniforms = new List<ShaderUniformVariableDeclaration>();
            List<ShaderLocalVariableDeclaration> locals = new List<ShaderLocalVariableDeclaration>();

            Dictionary<string, ShaderVariableDeclaration> stageInputs = new Dictionary<string, ShaderVariableDeclaration>();
            Dictionary<string, ShaderVariableDeclaration> stageOutputs = new Dictionary<string, ShaderVariableDeclaration>();

            List<MethodDeclarationSyntax> methods = new List<MethodDeclarationSyntax>();

            foreach (MemberDeclarationSyntax member in classDeclaration.Members)
            {
                switch (member)
                {
                    case FieldDeclarationSyntax field:
                        ShaderVariableDeclaration shaderVariable;
                        if (field.HasAttribute(ShaderGenerator.InAttributeFullName, model, cancellationToken))
                        {
                            inVars.Add(new ShaderInVariableDeclaration(field, diagnosticReporter, compilation, model, cancellationToken));
                            shaderVariable = inVars.Last();
                        }
                        else if (field.HasAttribute(ShaderGenerator.OutAttributeFullName, model, cancellationToken))
                        {
                            outVars.Add(new ShaderOutVariableDeclaration(field, diagnosticReporter, compilation, model, cancellationToken));
                            shaderVariable = outVars.Last();
                        }
                        else if (field.HasAttribute(ShaderGenerator.UniformAttributeFullName, model, cancellationToken))
                        {
                            ShaderUniformVariableDeclaration uniform = new ShaderUniformVariableDeclaration(field, diagnosticReporter, compilation, model, cancellationToken);
                            foreach (ShaderUniformVariableDeclaration otherUniform in uniforms)
                            {
                                if (uniform.Set == otherUniform.Set && uniform.Binding == otherUniform.Binding)
                                {
                                    IsValidForGeneration = false;
                                    diagnosticReporter.ReportError("1553", $"Uniform variable has the same Set and Binding as {otherUniform.Field.Declaration.GetText()}.", uniform.Field.GetLocation());
                                }
                                if (uniform.UniqueBinding == otherUniform.UniqueBinding)
                                {
                                    IsValidForGeneration = false;
                                    diagnosticReporter.ReportError("1554", $"Uniform variable has the same Unique Binding as {otherUniform.Field.Declaration.GetText()}.", uniform.Field.GetLocation());
                                }
                            }
                            uniforms.Add(uniform);
                            shaderVariable = uniform;
                        }
                        else
                        {
                            locals.Add(new ShaderLocalVariableDeclaration(field, diagnosticReporter, compilation, model, cancellationToken));
                            shaderVariable = locals.Last();
                        }

                        fields.Add(shaderVariable);

                        if (shaderVariable.StageVariable != null)
                        {
                            if (shaderVariable.StageVariable is ShaderStageInVariable shaderStageInput)
                            {
                                if (stageInputs.ContainsKey(shaderStageInput.Name))
                                {
                                    IsValidForGeneration = false;
                                    diagnosticReporter.ReportError("1581", $"Shader Stage variable \"{shaderStageInput.Name}\" is specified multiple times.", shaderVariable.Field.GetLocation());
                                }
                                else stageInputs[shaderStageInput.Name] = shaderVariable;
                            }
                            else if (shaderVariable.StageVariable is ShaderStageOutVariable shaderStageOutput)
                            {
                                if (stageOutputs.ContainsKey(shaderStageOutput.Name))
                                {
                                    IsValidForGeneration = false;
                                    diagnosticReporter.ReportError("1581", $"Shader Stage variable \"{shaderStageOutput.Name}\" is specified multiple times.", shaderVariable.Field.GetLocation());
                                }
                                else stageOutputs[shaderStageOutput.Name] = shaderVariable;
                            }
                        }
                        break;

                    case MethodDeclarationSyntax method:
                        methods.Add(method);
                        if (method.Identifier.Text == "Main")
                            EntryMethod = method;
                        break;
                }
            }

            if (!CheckStageVariables(mappings, diagnosticReporter, stageInputs.Values.Select(s => s.StageVariable as ShaderStageInVariable)!, stageOutputs.Values.Select(s => s.StageVariable as ShaderStageOutVariable)!))
                IsValidForGeneration = false;

            if (EntryMethod == null)
            {
                EntryMethod = null!;
                IsValidForGeneration = false;
                diagnosticReporter.ReportError("1700", $"Main Method not found in {Name} Shader Class.", classDeclaration.Identifier.GetLocation());
            }


            Fields = fields;
            InVars = inVars;
            OutVars = outVars;
            Uniforms = uniforms;
            Locals = locals;

            StageInputs = stageInputs;
            StageOutputs = stageOutputs;

            Methods = methods;

            IReadOnlyList<AttributeData> shaderBackendTargetAttributes = classDeclaration.GetAttributesData(ShaderGenerator.ShaderBackendTargetFullName, compilation, cancellationToken);
            if (shaderBackendTargetAttributes.Count > 0)
            {
                List<string> targetBackendNames = new List<string>();

                foreach (AttributeData shaderBackendTargetAttribute in shaderBackendTargetAttributes)
                    if (shaderBackendTargetAttribute.TryGetAttributeConstructorValues(0, out string[]? targetBackends))
                        foreach (string backendName in targetBackends)
                            if (BackendBuilders.IsBackendNameValid(backendName))
                                targetBackendNames.Add(backendName);
                            else
                            {
                                diagnosticReporter.ReportError("1800", $"Unknown Backend Name: {backendName}", classDeclaration.Identifier.GetLocation());
                                IsValidForGeneration = false;
                            }

                if (targetBackendNames.Count == 0)
                {
                    diagnosticReporter.ReportError("1801", $"No Backend Names provided for {Name} Shader Class.", classDeclaration.Identifier.GetLocation());
                    IsValidForGeneration = false;
                }
                TargetBackendNames = targetBackendNames;
            }
            else TargetBackendNames = BackendBuilders.DefaultBackendNames;
        }

        #endregion

        #region Private Methods

        private bool CheckStageVariables(ShaderLanguageMappings mappings, IDiagnosticReporter diagnosticReporter, IEnumerable<ShaderStageInVariable> stageInVariables, IEnumerable<ShaderStageOutVariable> stageOutVariables)
        {
            bool noErrors = true;

            foreach (ShaderStageInVariable stageInVariable in stageInVariables)
                if (mappings.StageInVariables.TryGetValue(stageInVariable.Name, out StageInVariableMapping? variableMapping))
                {
                    if (!CheckStageVariable(mappings, diagnosticReporter, stageInVariable, variableMapping))
                        noErrors = false;
                }
                else
                {
                    noErrors = false;
                    diagnosticReporter.ReportError("1505", $"Unknown Shader Stage Input variable Name: {stageInVariable.Name}", stageInVariable.Field.GetLocation());
                }


            foreach (ShaderStageOutVariable stageOutVariable in stageOutVariables)
                if (mappings.StageOutVariables.TryGetValue(stageOutVariable.Name, out StageOutVariableMapping? variableMapping))
                {
                    if (!CheckStageVariable(mappings, diagnosticReporter, stageOutVariable, variableMapping))
                        noErrors = false;
                }
                else
                {
                    noErrors = false;
                    diagnosticReporter.ReportError("1506", $"Unknown Shader Stage Output variable Name: {stageOutVariable.Name}", stageOutVariable.Field.GetLocation());
                }

            return noErrors;
        }

        #endregion

        #region Protected Methods

        protected virtual bool CheckStageVariable(ShaderLanguageMappings mappings, IDiagnosticReporter diagnosticReporter, ShaderStageVariable stageVariable, StageVariableMapping stageVariableMapping)
        {
            if (ASTAnalyzerHelper.TryGetShaderTypeMapping(Compilation, mappings, ClassTypeSymbol, stageVariable.Field.Declaration.Type, CancellationToken, out ShaderTypeMapping? mapping) && mapping.TypeMapping != null)
            {
                if (stageVariableMapping.ShaderType == mapping.TypeMapping)
                    return true;
                else
                {
                    diagnosticReporter.ReportError("1507", $"Incorrect Type of Shader Stage variable. Expected Mapped Shader Type: {stageVariableMapping.ShaderType.TypeName}, Actual Mapped Shader Type: {mapping.TypeMapping.TypeName}", stageVariable.Field.Declaration.Type.GetLocation());
                    return false;
                }
            }
            else
            {
                diagnosticReporter.ReportError("3000", $"Unknown Type: {stageVariable.Field.Declaration.Type.GetText()}", stageVariable.Field.Declaration.Type.GetLocation());
                return false;
            }
        }

        #endregion

    }
}
