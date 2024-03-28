using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using SharpGraphics.Shaders.Generator;

namespace SharpGraphics.Shaders.Mappings
{
    public class ShaderLanguageMappings
    {

        #region Shader Type Fields

        private Dictionary<string, PrimitiveShaderType> _primitiveTypes = new Dictionary<string, PrimitiveShaderType>();
        private Dictionary<string, VectorShaderType> _vectorTypes = new Dictionary<string, VectorShaderType>();
        private Dictionary<string, MatrixShaderType> _matrixTypes = new Dictionary<string, MatrixShaderType>();
        private Dictionary<string, TextureSamplerType> _textureTypes = new Dictionary<string, TextureSamplerType>();

        private Dictionary<string, ShaderType> _types = new Dictionary<string, ShaderType>();

        private Dictionary<string, ShaderStatementExpression> _statements = new Dictionary<string, ShaderStatementExpression>();
        private Dictionary<string, ShaderOperatorExpression> _operators = new Dictionary<string, ShaderOperatorExpression>();
        private Dictionary<string, ShaderFunctionExpression> _functions = new Dictionary<string, ShaderFunctionExpression>();

        private Dictionary<string, StageInVariableMapping> _stageInVariables = new Dictionary<string, StageInVariableMapping>();
        private Dictionary<string, StageOutVariableMapping> _stageOutVariables = new Dictionary<string, StageOutVariableMapping>();

        #endregion

        #region Shader Type Properties

        public VoidShaderType VoidType { get; } = new VoidShaderType();

        public BoolShaderType BoolType { get; } = new BoolShaderType();

        public IntShaderType Int8Type { get; } = new IntShaderType(ShaderTypePrecisions.Bits8);
        public IntShaderType Int16Type { get; } = new IntShaderType(ShaderTypePrecisions.Bits16);
        public IntShaderType Int32Type { get; } = new IntShaderType(ShaderTypePrecisions.Bits32);
        public IntShaderType Int64Type { get; } = new IntShaderType(ShaderTypePrecisions.Bits64);
        public UIntShaderType UInt8Type { get; } = new UIntShaderType(ShaderTypePrecisions.Bits8);
        public UIntShaderType UInt16Type { get; } = new UIntShaderType(ShaderTypePrecisions.Bits16);
        public UIntShaderType UInt32Type { get; } = new UIntShaderType(ShaderTypePrecisions.Bits32);
        public UIntShaderType UInt64Type { get; } = new UIntShaderType(ShaderTypePrecisions.Bits64);
        public FloatShaderType Float16Type { get; } = new FloatShaderType(ShaderTypePrecisions.Bits16);
        public FloatShaderType Float32Type { get; } = new FloatShaderType(ShaderTypePrecisions.Bits32);
        public FloatShaderType Float64Type { get; } = new FloatShaderType(ShaderTypePrecisions.Bits64);
        

        public IReadOnlyDictionary<string, PrimitiveShaderType> PrimitiveTypes => _primitiveTypes;
        public IReadOnlyDictionary<string, VectorShaderType> VectorTypes => _vectorTypes;
        public IReadOnlyDictionary<string, MatrixShaderType> MatrixTypes => _matrixTypes;
        public IReadOnlyDictionary<string, TextureSamplerType> TextureTypes => _textureTypes;

        public IReadOnlyDictionary<string, ShaderType> Types => _types;

        public IReadOnlyDictionary<string, ShaderStatementExpression> Statements => _statements;
        public IReadOnlyDictionary<string, ShaderOperatorExpression> Operators => _operators;
        public IReadOnlyDictionary<string, ShaderFunctionExpression> Functions => _functions;

        internal IReadOnlyDictionary<string, StageInVariableMapping> StageInVariables => _stageInVariables; //Should these be part of ShaderClassMapping's FieldMappings?
        internal IReadOnlyDictionary<string, StageOutVariableMapping> StageOutVariables => _stageOutVariables;

        #endregion

        #region Shader Mapping Properties

        public ShaderTypeMapping ShaderClassMapping { get; }
        public IReadOnlyDictionary<SpecialType, ShaderTypeMapping> SpecialTypeMappings { get; }
        public IReadOnlyDictionary<string, ShaderTypeMapping> TypeMappings { get; }

        #endregion

        #region Constructors

        internal ShaderLanguageMappings()
        {
            //SHADER LANGUAGE TYPES AND FUNCTIONS
            //PRIMITIVES
            AddShaderType(VoidType);

            AddShaderType(BoolType);
            AddOperator(new ShaderUnaryOperatorExpression(BoolType, "!", true, false));
            AddOperator(new ShaderBinaryOperatorExpression(BoolType, BoolType, "==", true));
            AddOperator(new ShaderBinaryOperatorExpression(BoolType, BoolType, "!=", true));
            AddOperator(new ShaderBinaryOperatorExpression(BoolType, BoolType, "&&", true));
            AddOperator(new ShaderBinaryOperatorExpression(BoolType, BoolType, "||", true));

            //Numerics
            AddShaderType(Int8Type);
            AddShaderType(Int16Type);
            AddShaderType(Int32Type);
            AddShaderType(Int64Type);
            AddShaderType(UInt8Type);
            AddShaderType(UInt16Type);
            AddShaderType(UInt32Type);
            AddShaderType(UInt64Type);
            AddShaderType(Float16Type);
            AddShaderType(Float32Type);
            AddShaderType(Float64Type);
            AddImplicitConversions(new ShaderType[] { BoolType, Int8Type, Int16Type, Int32Type, Int64Type, UInt8Type, UInt16Type, UInt32Type, UInt64Type, Float16Type, Float32Type, Float64Type });
            NumericShaderType[] numericShaderTypes = new NumericShaderType[] { Int8Type, Int16Type, Int32Type, Int64Type, UInt8Type, UInt16Type, UInt32Type, UInt64Type, Float16Type, Float32Type, Float64Type };
            AddSelfOperators(numericShaderTypes);
            AddMathFunctions(new ShaderType[] { Float32Type });

            //VECTORS
            //Vector2
            ShaderType[] vectorComponentTypes = new ShaderType[] { Int32Type, UInt32Type, Float32Type };
            IEnumerable<ShaderType> vector2Types = new GenericVectorShaderType(2u).TryCreateTypes(vectorComponentTypes);
            AddImplicitConversions(vector2Types);
            AddShaderTypes(vector2Types);
            //Vector3
            IEnumerable<ShaderType> vector3Types = new GenericVectorShaderType(3u).TryCreateTypes(vectorComponentTypes);
            AddImplicitConversions(vector3Types);
            foreach (ShaderType vector3Type in vector3Types)
                if (vector3Type is Vector3ShaderType vector3ShaderType)
                {
                    vector3ShaderType.AddExpressionsFor((_vectorTypes[$"Vector2{vector3ShaderType.ComponentType.TypeName}"] as Vector2ShaderType)!);
                }
            AddShaderTypes(vector3Types);
            //Vector4
            IEnumerable<ShaderType> vector4Types = new GenericVectorShaderType(4u).TryCreateTypes(vectorComponentTypes);
            AddImplicitConversions(vector4Types);
            foreach (ShaderType vector4Type in vector4Types)
                if (vector4Type is Vector4ShaderType vector4ShaderType)
                {
                    vector4ShaderType.AddExpressionsFor((_vectorTypes[$"Vector2{vector4ShaderType.ComponentType.TypeName}"] as Vector2ShaderType)!, (_vectorTypes[$"Vector3{vector4ShaderType.ComponentType.TypeName}"] as Vector3ShaderType)!);
                }
            AddShaderTypes(vector4Types);

            VectorShaderType[] vectorFloat32Types = new VectorShaderType[] { _vectorTypes["Vector2Float32"], _vectorTypes["Vector3Float32"], _vectorTypes["Vector4Float32"], };
            AddVectorFunctions(vectorFloat32Types);
            AddMathFunctions(vectorFloat32Types);
            AddVectorMathFunctionsWithComponents(vectorFloat32Types);

            //MATRICES
            //Matrix4x4
            AddShaderTypes(new GenericMatrixShaderType(4u).TryCreateTypes(new ShaderType[] { _vectorTypes["Vector4Float32"], }));
            AddImplicitConversions(_matrixTypes.Values.Where(v => v.RowCount == 4u && v.ColumnType.ComponentCount == 4u));

            //VECTOR AND MATRIX OPERATORS
            AddSelfOperators(_vectorTypes.Values);
            AddVectorSelfOperators(_vectorTypes.Values);
            AddOperator(new ShaderBinaryOperatorExpression(_vectorTypes["Vector4Float32"], _matrixTypes["Matrix4x4Float32"], "*", false));
            AddOperator(new ShaderBinaryOperatorExpression(_matrixTypes["Matrix4x4Float32"], _vectorTypes["Vector4Float32"], "*", false));

            //TEXTURESAMPLERS
            IEnumerable<ShaderType> textureSamplerTypes = vector4Types;
            IEnumerable<ShaderType> texture2DTypes = new GenericTextureSamplerShaderType(TextureSamplerDimensions.Dimensions2).TryCreateTypes(textureSamplerTypes);
            AddShaderTypes(texture2DTypes);
            AddImplicitConversions(texture2DTypes);
            IEnumerable<ShaderType> textureCubeTypes = new GenericTextureSamplerShaderType(TextureSamplerDimensions.Cube).TryCreateTypes(textureSamplerTypes);
            AddShaderTypes(textureCubeTypes);
            AddImplicitConversions(textureCubeTypes);
            IEnumerable<ShaderType> textureRenderPassInputTypes = new GenericTextureSamplerShaderType(TextureSamplerDimensions.RenderPassInput).TryCreateTypes(textureSamplerTypes);
            AddShaderTypes(textureRenderPassInputTypes);
            AddImplicitConversions(textureRenderPassInputTypes);

            AddFunction(new ShaderFunctionExpression("textureSize", texture2DTypes.First()));
            AddFunction(new ShaderFunctionExpression("textureSize", textureCubeTypes.First()));
            AddFunction(new ShaderFunctionExpression("texelFetch", new[] { texture2DTypes.First(), _vectorTypes["Vector2Int32"] }));
            AddFunction(new ShaderFunctionExpression("texelSample", new[] { texture2DTypes.First(), _vectorTypes["Vector2Float32"] }));
            AddFunction(new ShaderFunctionExpression("texelSample", new[] { textureCubeTypes.First(), _vectorTypes["Vector3Float32"] }));
            AddFunction(new ShaderFunctionExpression("subpassFetch", textureRenderPassInputTypes.First()));


            //BUILT-IN FUNCTIONS
            AddStatement(new ShaderStatementExpression("discard"));


            //STAGE VARIABLE MAPPINGS
            //Vertex Shader Inputs
            MapStageVariable(new StageInVariableMapping("vID", ShaderStage.Vertex, UInt32Type));
            //Vertex Shader Outputs
            MapStageVariable(new StageOutVariableMapping("vPosition", ShaderStage.Vertex, _vectorTypes["Vector4Float32"]));
            MapStageVariable(new StageOutVariableMapping("vPointSize", ShaderStage.Vertex, Float32Type));
            //TODO: Shader Array Type: AddStageVariable(new StageOutVariable("vClipDistance", ShaderStage.Vertex, Float32Type));

            //Fragment Shader Inputs
            MapStageVariable(new StageInVariableMapping("fCoord", ShaderStage.Fragment, _vectorTypes["Vector4Float32"]));
            MapStageVariable(new StageInVariableMapping("fIsFrontFace", ShaderStage.Fragment, BoolType));
            MapStageVariable(new StageInVariableMapping("fPointCoord", ShaderStage.Fragment, _vectorTypes["Vector2Float32"]));
            //TODO: Shader Array Type: AddStageVariable(new StageInVariable("fClipDistance", ShaderStage.Fragment, Float32Type));
            //Fragment Shader Outputs
            MapStageVariable(new StageOutVariableMapping("fDepth", ShaderStage.Fragment, Float32Type));


            //DEFAULT MAPPINGS
            ShaderClassMapping = new ShaderTypeMapping();
            ShaderClassMapping.AddMethodMapping(new ShaderTypeMethodMapping(Statements["discard"], "Discard"));
            ShaderTypeMapping MathMapping = new ShaderTypeMapping();

            SpecialTypeMappings = new Dictionary<SpecialType, ShaderTypeMapping>()
            {
                { SpecialType.System_Void, new ShaderTypeMapping(VoidType) },

                { SpecialType.System_Boolean, new ShaderTypeMapping(BoolType).MatchOperators(Operators.Values) },

                { SpecialType.System_SByte, new ShaderTypeMapping(Int8Type).MatchOperators(Operators.Values) },
                { SpecialType.System_Int16, new ShaderTypeMapping(Int16Type).MatchOperators(Operators.Values) },
                { SpecialType.System_Int32, new ShaderTypeMapping(Int32Type).MatchOperators(Operators.Values) },
                { SpecialType.System_Int64, new ShaderTypeMapping(Int64Type).MatchOperators(Operators.Values) },
                { SpecialType.System_Byte, new ShaderTypeMapping(UInt8Type).MatchOperators(Operators.Values) },
                { SpecialType.System_UInt16, new ShaderTypeMapping(UInt16Type).MatchOperators(Operators.Values) },
                { SpecialType.System_UInt32, new ShaderTypeMapping(UInt32Type).MatchOperators(Operators.Values) },
                { SpecialType.System_UInt64, new ShaderTypeMapping(UInt64Type).MatchOperators(Operators.Values) },
                { SpecialType.System_Single, new ShaderTypeMapping(Float32Type).MatchOperators(Operators.Values) },
                { SpecialType.System_Double, new ShaderTypeMapping(Float64Type).MatchOperators(Operators.Values) },
            };
            MapMathFunctions(MathMapping, Float32Type);

            TypeMappings = new Dictionary<string, ShaderTypeMapping>()
            {
                { "System.Numerics.Vector2", new ShaderTypeMapping(_vectorTypes["Vector2Float32"]).MatchConstructors().MatchOperators(Operators.Values).MatchMembersAsMethods(2).MatchFields(("x","X"), ("y","Y")) },
                { "System.Numerics.Vector3", new ShaderTypeMapping(_vectorTypes["Vector3Float32"]).MatchConstructors().MatchOperators(Operators.Values).MatchMembersAsMethods(2).MatchFields(("x","X"), ("y","Y"), ("z","Z")) },
                { "System.Numerics.Vector4", new ShaderTypeMapping(_vectorTypes["Vector4Float32"]).MatchConstructors().MatchOperators(Operators.Values).MatchMembersAsMethods(2).MatchFields(("x","X"), ("y","Y"), ("z","Z"), ("w","W")) },
                { "System.Numerics.Matrix4x4", new ShaderTypeMapping(_matrixTypes["Matrix4x4Float32"]).MatchConstructors().MatchOperators(Operators.Values) },

                { "SharpGraphics.Utils.Vector2Int", new ShaderTypeMapping(_vectorTypes["Vector2Int32"]).MatchConstructors().MatchOperators(Operators.Values).MatchMembersAsMethods(2).MatchFields(("x","X"), ("y","Y")) },
                { "SharpGraphics.Utils.Vector3Int", new ShaderTypeMapping(_vectorTypes["Vector3Int32"]).MatchConstructors().MatchOperators(Operators.Values).MatchMembersAsMethods(2).MatchFields(("x","X"), ("y","Y"), ("z","Z")) },
                { "SharpGraphics.Utils.Vector4Int", new ShaderTypeMapping(_vectorTypes["Vector4Int32"]).MatchConstructors().MatchOperators(Operators.Values).MatchMembersAsMethods(2).MatchFields(("x","X"), ("y","Y"), ("z","Z"), ("w","W")) },

                { "SharpGraphics.Utils.Vector2UInt", new ShaderTypeMapping(_vectorTypes["Vector2UInt32"]).MatchConstructors().MatchOperators(Operators.Values).MatchMembersAsMethods(2).MatchFields(("x","X"), ("y","Y")) },
                { "SharpGraphics.Utils.Vector3UInt", new ShaderTypeMapping(_vectorTypes["Vector3UInt32"]).MatchConstructors().MatchOperators(Operators.Values).MatchMembersAsMethods(2).MatchFields(("x","X"), ("y","Y"), ("z","Z")) },
                { "SharpGraphics.Utils.Vector4UInt", new ShaderTypeMapping(_vectorTypes["Vector4UInt32"]).MatchConstructors().MatchOperators(Operators.Values).MatchMembersAsMethods(2).MatchFields(("x","X"), ("y","Y"), ("z","Z"), ("w","W")) },

                { "SharpGraphics.Shaders.TextureSampler2D", new ShaderTypeMapping(_textureTypes["Texture2DVector4Float32"]) },
                { "SharpGraphics.Shaders.TextureSamplerCube", new ShaderTypeMapping(_textureTypes["TextureCubeVector4Float32"]) },
                { "SharpGraphics.Shaders.RenderPassInput", new ShaderTypeMapping(_textureTypes["TextureRenderPassInputVector4Float32"]) },

                { "System.Math", MathMapping },
                { "System.MathF", MathMapping },
                { "SharpGraphics.Utils.MathF", MathMapping },
                { "SharpGraphics.Utils.MathHelper", MathMapping },
            };

            MapMathFunctions(MathMapping, _vectorTypes["Vector2Float32"]);
            MapMathFunctions(MathMapping, _vectorTypes["Vector3Float32"]);
            MapMathFunctions(MathMapping, _vectorTypes["Vector4Float32"]);
            MapVectorFunctions(TypeMappings["System.Numerics.Vector2"]);
            MapVectorFunctions(TypeMappings["System.Numerics.Vector3"]);
            MapVectorFunctions(TypeMappings["System.Numerics.Vector4"]);
            TypeMappings["System.Numerics.Vector4"].AddMethodMapping(new ShaderTypeMethodMapping(Operators["Vector4Float32*Matrix4x4Float32"], "Transform", new ShaderArgumentMappings(new ShaderArgumentMapping(1, _vectorTypes["Vector4Float32"]), new ShaderArgumentMapping(0, _matrixTypes["Matrix4x4Float32"]))));

            TypeMappings["SharpGraphics.Shaders.TextureSampler2D"].AddMethodMapping(new ShaderTypeMethodMapping(Functions["texelSample(Texture2DVector4Int32,Vector2Float32)"], "Sample", new ShaderArgumentMappings(0, new ShaderArgumentMapping(1, _vectorTypes["Vector2Float32"]))));
            TypeMappings["SharpGraphics.Shaders.TextureSamplerCube"].AddMethodMapping(new ShaderTypeMethodMapping(Functions["texelSample(TextureCubeVector4Int32,Vector3Float32)"], "Sample", new ShaderArgumentMappings(0, new ShaderArgumentMapping(1, _vectorTypes["Vector3Float32"]))));
            TypeMappings["SharpGraphics.Shaders.RenderPassInput"].AddMethodMapping(new ShaderTypeMethodMapping(Functions["subpassFetch(TextureRenderPassInputVector4Int32)"], "Load", new ShaderArgumentMappings(0)));
        }

        #endregion

        #region Private Methods

        private void AddShaderType(ShaderType type)
        {
            _types[type.TypeName] = type;
            switch (type)
            {
                case PrimitiveShaderType primitive: _primitiveTypes[type.TypeName] = primitive; break;
                case VectorShaderType vector: _vectorTypes[type.TypeName] = vector; break;
                case MatrixShaderType matrix: _matrixTypes[type.TypeName] = matrix; break;
                case TextureSamplerType texture: _textureTypes[type.TypeName] = texture; break;
            }
        }
        private void AddShaderTypes(IEnumerable<ShaderType> types)
        {
            foreach (ShaderType type in types)
                AddShaderType(type);
        }

        private void AddImplicitConversions(IEnumerable<ShaderType> types)
        {
            foreach (ShaderType type in types)
                foreach (ShaderType targetType in types)
                    if (type != targetType)
                        type.AddImplicitConversionTarget(targetType);
        }

        private void AddOperator(ShaderOperatorExpression op) => _operators[op.ExpressionName] = op;
        private void AddOperators(IEnumerable<ShaderOperatorExpression> ops)
        {
            foreach (ShaderOperatorExpression op in ops)
                AddOperator(op);
        }
        private void AddSelfOperators(IEnumerable<ShaderType> types)
        {
            foreach (ShaderType type in types)
            {
                AddOperator(new ShaderUnaryOperatorExpression(type, "-", true, false));
                AddOperator(new ShaderUnaryOperatorExpression(type, "++", true, true));
                AddOperator(new ShaderUnaryOperatorExpression(type, "--", true, true));

                AddOperator(new ShaderBinaryOperatorExpression(type, type, "+", true));
                AddOperator(new ShaderBinaryOperatorExpression(type, type, "-", true));
                AddOperator(new ShaderBinaryOperatorExpression(type, type, "*", true));
                AddOperator(new ShaderBinaryOperatorExpression(type, type, "/", true));
                AddOperator(new ShaderBinaryOperatorExpression(type, type, "==", true));
                AddOperator(new ShaderBinaryOperatorExpression(type, type, "!=", true));
                AddOperator(new ShaderBinaryOperatorExpression(type, type, "<", true));
                AddOperator(new ShaderBinaryOperatorExpression(type, type, "<=", true));
                AddOperator(new ShaderBinaryOperatorExpression(type, type, ">", true));
                AddOperator(new ShaderBinaryOperatorExpression(type, type, ">=", true));
            }
        }
        private void AddVectorSelfOperators(IEnumerable<VectorShaderType> types)
        {
            foreach (VectorShaderType type in types)
            {
                AddOperator(new ShaderBinaryOperatorExpression(type, type.ComponentType, "+", true));
                AddOperator(new ShaderBinaryOperatorExpression(type, type.ComponentType, "-", true));
                AddOperator(new ShaderBinaryOperatorExpression(type, type.ComponentType, "*", true));
                AddOperator(new ShaderBinaryOperatorExpression(type, type.ComponentType, "/", true));
                AddOperator(new ShaderBinaryOperatorExpression(type, type.ComponentType, "==", true));
                AddOperator(new ShaderBinaryOperatorExpression(type, type.ComponentType, "!=", true));
                AddOperator(new ShaderBinaryOperatorExpression(type, type.ComponentType, "<", true));
                AddOperator(new ShaderBinaryOperatorExpression(type, type.ComponentType, "<=", true));
                AddOperator(new ShaderBinaryOperatorExpression(type, type.ComponentType, ">", true));
                AddOperator(new ShaderBinaryOperatorExpression(type, type.ComponentType, ">=", true));
            }
        }

        private void AddStatement(ShaderStatementExpression statement) => _statements[statement.ExpressionName] = statement;

        private void AddFunction(ShaderFunctionExpression function) => _functions[function.ExpressionName] = function;
        private void AddFunctions(IEnumerable<ShaderFunctionExpression> functions)
        {
            foreach (ShaderFunctionExpression function in functions)
                AddFunction(function);
        }
        private void AddMathFunctions(IEnumerable<ShaderType> types)
        {
            foreach (ShaderType type in types)
            {
                //GLSL 8.1 Angle and Trigonometry Functions
                AddFunction(new ShaderFunctionExpression("radians", type));
                AddFunction(new ShaderFunctionExpression("degrees", type));
                AddFunction(new ShaderFunctionExpression("sin", type));
                AddFunction(new ShaderFunctionExpression("cos", type));
                AddFunction(new ShaderFunctionExpression("tan", type));
                AddFunction(new ShaderFunctionExpression("asin", type));
                AddFunction(new ShaderFunctionExpression("acos", type));
                AddFunction(new ShaderFunctionExpression("atan", type));
                AddFunction(new ShaderFunctionExpression("atan2", type, type));
                AddFunction(new ShaderFunctionExpression("sinh", type));
                AddFunction(new ShaderFunctionExpression("cosh", type));
                AddFunction(new ShaderFunctionExpression("tanh", type));
                AddFunction(new ShaderFunctionExpression("asinh", type));
                AddFunction(new ShaderFunctionExpression("acosh", type));
                AddFunction(new ShaderFunctionExpression("atanh", type));

                //GLSL 8.2 Exponential Functions
                AddFunction(new ShaderFunctionExpression("pow", type, type));
                AddFunction(new ShaderFunctionExpression("exp", type));
                AddFunction(new ShaderFunctionExpression("exp2", type));
                AddFunction(new ShaderFunctionExpression("log", type));
                AddFunction(new ShaderFunctionExpression("log2", type));
                AddFunction(new ShaderFunctionExpression("sqrt", type));
                AddFunction(new ShaderFunctionExpression("inversesqrt", type));

                //GLSL 8.3 Common Functions
                AddFunction(new ShaderFunctionExpression("abs", type));
                AddFunction(new ShaderFunctionExpression("sign", type));
                AddFunction(new ShaderFunctionExpression("floor", type));
                AddFunction(new ShaderFunctionExpression("truncate", type));
                AddFunction(new ShaderFunctionExpression("round", type));
                //TODO: GLSL: roundEven
                AddFunction(new ShaderFunctionExpression("ceil", type));
                AddFunction(new ShaderFunctionExpression("fract", type));
                AddFunction(new ShaderFunctionExpression("mod", type, type));
                //TODO: GLSL: modf
                AddFunction(new ShaderFunctionExpression("min", type, type));
                AddFunction(new ShaderFunctionExpression("max", type, type));
                AddFunction(new ShaderFunctionExpression("clamp", type, type, type));
                AddFunction(new ShaderFunctionExpression("mix", type, type, type));
                AddFunction(new ShaderFunctionExpression("step", type, type));
                AddFunction(new ShaderFunctionExpression("smoothstep", type, type, type));

                //TODO: GLSL: floatBitsToInt/Uint, vica-versa

                //TODO: GLSL 8.4 Floating-Point Pack and unpack Functions
            }
        }
        private void AddVectorMathFunctionsWithComponents(IEnumerable<VectorShaderType> types)
        {
            foreach (VectorShaderType type in types)
            {
                //GLSL 8.1 Angle and Trigonometry Functions

                //GLSL 8.3 Common Functions
                //TODO: GLSL: roundEven
                AddFunction(new ShaderFunctionExpression("mod", type, type.ComponentType));
                //TODO: GLSL: modf
                AddFunction(new ShaderFunctionExpression("min", type, type.ComponentType));
                AddFunction(new ShaderFunctionExpression("max", type, type.ComponentType));
                AddFunction(new ShaderFunctionExpression("mix", type, type, type.ComponentType));
                AddFunction(new ShaderFunctionExpression("step", type, type.ComponentType));
                AddFunction(new ShaderFunctionExpression("smoothstep", type, type, type.ComponentType));

                //TODO: GLSL: floatBitsToInt/Uint, vica-versa

                //TODO: GLSL 8.4 Floating-Point Pack and unpack Functions

                //GLSL 8.5 Geometric Functions
            }
        }
        private void AddVectorFunctions(IEnumerable<VectorShaderType> types)
        {
            foreach (VectorShaderType type in types)
            {
                //GLSL 8.5 Geometric Functions
                AddFunction(new ShaderFunctionExpression("length", type));
                AddFunction(new ShaderFunctionExpression("distance", type, type));
                AddFunction(new ShaderFunctionExpression("dot", type, type ));
                AddFunction(new ShaderFunctionExpression("cross",  type, type));
                AddFunction(new ShaderFunctionExpression("normalize", type));
                AddFunction(new ShaderFunctionExpression("faceforward", type, type, type)); //TODO: Some just for floating-point component types
                AddFunction(new ShaderFunctionExpression("reflect", type, type));
                AddFunction(new ShaderFunctionExpression("refract", type, type, Float32Type));
                AddFunction(new ShaderFunctionExpression("refract", type, type, Float64Type));
            }
        }


        private void MapMathFunctions(ShaderTypeMapping mapping)
        {
            if (mapping.TypeMapping != null)
                MapMathFunctions(mapping, mapping.TypeMapping);
        }
        private void MapMathFunctions(ShaderTypeMapping mapping, ShaderType targetType)
        {
            //GLSL 8.1 Angle and Trigonometry Functions
            //AddFunction(new ShaderFunctionExpression("radians", type));
            //AddFunction(new ShaderFunctionExpression("degrees", type));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"sin({targetType.TypeName})"], "Sin", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"cos({targetType.TypeName})"], "Cos", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"tan({targetType.TypeName})"], "Tan", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"asin({targetType.TypeName})"], "Asin", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"acos({targetType.TypeName})"], "Acos", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"atan({targetType.TypeName})"], "Atan", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"atan2({targetType.TypeName},{targetType.TypeName})"], "Atan2", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType), new ShaderArgumentMapping(1, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"sinh({targetType.TypeName})"], "Sinh", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"cosh({targetType.TypeName})"], "Cosh", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"tanh({targetType.TypeName})"], "Tanh", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"asinh({targetType.TypeName})"], "Asinh", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"acosh({targetType.TypeName})"], "Acosh", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            //mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"Atanh({targetType.TypeName})"], "Atanh", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));

            //GLSL 8.2 Exponential Functions
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"pow({targetType.TypeName},{targetType.TypeName})"], "Pow", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType), new ShaderArgumentMapping(1, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"exp({targetType.TypeName})"], "exp", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"exp2({targetType.TypeName})"], "Exp2", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"log({targetType.TypeName})"], "Log", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"log2({targetType.TypeName})"], "Log2", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"sqrt({targetType.TypeName})"], "Sqrt", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"inversesqrt({targetType.TypeName})"], "InverseSqrt", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));

            //GLSL 8.3 Common Functions

            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"abs({targetType.TypeName})"], "Abs", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"sign({targetType.TypeName})"], "Sign", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"floor({targetType.TypeName})"], "Floor", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"truncate({targetType.TypeName})"], "Truncate", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"round({targetType.TypeName})"], "Round", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            //TODO: GLSL: roundEven
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"ceil({targetType.TypeName})"], "Ceiling", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"fract({targetType.TypeName})"], "Fract", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"mod({targetType.TypeName},{targetType.TypeName})"], "Modulus", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType), new ShaderArgumentMapping(1, targetType))));
            //mapping.AddOperatorMapping(new ShaderTypeBinaryOperatorMapping(Functions[$"mod({targetType.TypeName},{targetType.TypeName})"], targetType, targetType, "%", false));
            //TODO: GLSL: modf
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"min({targetType.TypeName},{targetType.TypeName})"], "Min", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType), new ShaderArgumentMapping(1, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"max({targetType.TypeName},{targetType.TypeName})"], "Max", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType), new ShaderArgumentMapping(1, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"clamp({targetType.TypeName},{targetType.TypeName},{targetType.TypeName})"], "Clamp", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType), new ShaderArgumentMapping(1, targetType), new ShaderArgumentMapping(2, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"mix({targetType.TypeName},{targetType.TypeName},{targetType.TypeName})"], "Mix", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType), new ShaderArgumentMapping(1, targetType), new ShaderArgumentMapping(2, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"step({targetType.TypeName},{targetType.TypeName})"], "Step", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType), new ShaderArgumentMapping(1, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"smoothstep({targetType.TypeName},{targetType.TypeName},{targetType.TypeName})"], "Smoothstep", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType), new ShaderArgumentMapping(1, targetType), new ShaderArgumentMapping(2, targetType))));

            //TODO: GLSL: floatBitsToInt/Uint, vica-versa

            //TODO: GLSL 8.4 Floating-Point Pack and unpack Functions
        }
        private void MapVectorFunctions(ShaderTypeMapping mapping)
        {
            if (mapping.TypeMapping != null && mapping.TypeMapping is VectorShaderType vectorShaderType)
                MapVectorFunctions(mapping, vectorShaderType);
        }
        private void MapVectorFunctions(ShaderTypeMapping mapping, VectorShaderType targetType)
        {
            //GLSL 8.1 Angle and Trigonometry Functions

            //GLSL 8.3 Common Functions
            //TODO: GLSL: roundEven
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"mod({targetType.TypeName},{targetType.ComponentType})"], "Mod", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType), new ShaderArgumentMapping(1, targetType.ComponentType))));
            //mapping.AddOperatorMapping(new ShaderTypeBinaryOperatorMapping(Functions[$"mod({targetType.TypeName},{targetType.ComponentType})"], targetType, targetType.ComponentType, "%", false));
            //TODO: GLSL: modf
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"min({targetType.TypeName},{targetType.ComponentType})"], "Min", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType), new ShaderArgumentMapping(1, targetType.ComponentType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"max({targetType.TypeName},{targetType.ComponentType})"], "Max", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType), new ShaderArgumentMapping(1, targetType.ComponentType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"mix({targetType.TypeName},{targetType.TypeName},{targetType.ComponentType})"], "Mix", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType), new ShaderArgumentMapping(1, targetType), new ShaderArgumentMapping(2, targetType.ComponentType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"step({targetType.TypeName},{targetType.ComponentType})"], "Step", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType), new ShaderArgumentMapping(1, targetType.ComponentType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"smoothstep({targetType.TypeName},{targetType.TypeName},{targetType.ComponentType})"], "Smoothstep", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType), new ShaderArgumentMapping(1, targetType), new ShaderArgumentMapping(2, targetType.ComponentType))));

            //TODO: GLSL: floatBitsToInt/Uint, vica-versa

            //TODO: GLSL 8.4 Floating-Point Pack and unpack Functions

            //GLSL 8.5 Geometric Functions
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"length({targetType.TypeName})"], "Length", new ShaderArgumentMappings(0)));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"length({targetType.TypeName})"], "Length", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"distance({targetType.TypeName},{targetType.TypeName})"], "Distance", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType), new ShaderArgumentMapping(1, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"dot({targetType.TypeName},{targetType.TypeName})"], "Dot", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType), new ShaderArgumentMapping(1, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"cross({targetType.TypeName},{targetType.TypeName})"], "Cross", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType), new ShaderArgumentMapping(1, targetType))));
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"normalize({targetType.TypeName})"], "Normalize", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType))));
            //faceforward?
            mapping.AddMethodMapping(new ShaderTypeMethodMapping(Functions[$"reflect({targetType.TypeName},{targetType.TypeName})"], "Reflect", new ShaderArgumentMappings(new ShaderArgumentMapping(0, targetType), new ShaderArgumentMapping(1, targetType))));
            //refract?
            //refract?
        }


        private void MapStageVariable(StageVariableMapping variable)
        {
            switch (variable)
            {
                case StageInVariableMapping stageIn: _stageInVariables[stageIn.Name] = stageIn; break;
                case StageOutVariableMapping stageOut: _stageOutVariables[stageOut.Name] = stageOut; break;
            }
        }

        #endregion

        #region Public Methods


        #endregion

    }
}
