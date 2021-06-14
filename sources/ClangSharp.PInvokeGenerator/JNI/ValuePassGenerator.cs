// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using ClangSharp.Abstractions;
using ClangSharp.Interop;
using ClangSharp.JNI.Java;

namespace ClangSharp.JNI
{
    internal class ValuePassGenerator
    {
        private readonly JniGenerationPlan _generationPlan;

        public ValuePassGenerator(JniGenerationPlan generationPlan)
        {
            _generationPlan = generationPlan;
        }

        public VariableToGenerate ReturnTypeVariable { get; set; }
        public StandaloneValuePass ReturnTypePass { get; set; }

        public List<VariableToGenerate> ParameterVariables { get; } = new();
        public List<ValuePass> ParameterPasses { get; } = new();

        public GeneratedReturnTypes GeneratedReturnTypes => new(ReturnTypeVariable);
        public GeneratedParameters GeneratedParameters => new(ParameterVariables);

        public void ConsumeFunctionParameters(IEnumerable<MethodParameter<TypeDesc>> parameters, string methodName)
        {
            const ValuePassContext Context = ValuePassContext.MethodParameter;

            MethodParameter<TypeDesc>? pendingSupportedFunctionPointer = null;
            foreach (var parameter in parameters)
            {
                if (pendingSupportedFunctionPointer is { } prevFunctionPointerParameter)
                {
                    if (IsEligibleFunctionPointerContext(parameter.Type))
                    {
                        ConsumeFunctionPointerParameter(
                            prevFunctionPointerParameter, methodName);
                        pendingSupportedFunctionPointer = null;
                        continue;
                    }

                    throw new UnsupportedJniScenarioException(
                        "No void* argument after a function pointer.");
                }

                if (IsEligibleFunctionPointer(parameter.Type, out var isFunctionPointer))
                {
                    pendingSupportedFunctionPointer = parameter;
                    continue;
                }

                if (isFunctionPointer)
                {
                    throw new UnsupportedJniScenarioException(
                        "Function pointer does not have a void* parameter.");
                }


                Consume(parameter.Type, parameter.Name, Context);
            }
        }

        private static bool IsEligibleFunctionPointerContext(TypeDesc type) =>
            type is PointerTypeDesc {
                PointeeType: BuiltinTypeDesc { Kind: CXTypeKind.CXType_Void }
            };

        private static bool IsEligibleFunctionPointer(TypeDesc type, out bool isFunctionPointer)
        {
            if (type is PointerTypeDesc {
                PointeeType: FunctionProtoTypeDesc {
                    Parameters: { Count: > 0 }
                } functionType
            })
            {
                isFunctionPointer = true;
                return IsEligibleFunctionPointerContext(functionType.Parameters[^1]);
            }

            isFunctionPointer = false;
            return false;
        }

        public void Consume(TypeDesc type, string valueName, ValuePassContext context)
        {
            VariableToGenerate mainVariable;
            var mainPass = type switch {
                BuiltinTypeDesc { Kind: CXTypeKind.CXType_Void } => PassVoid(),
                PointerTypeDesc { PointeeType: { Kind: CXTypeKind.CXType_Char_S } } => PassString(),
                PointerTypeDesc pointerType => PassPointer(pointerType),
                BuiltinTypeDesc builtinType => PassPrimitive(builtinType),
                RecordTypeDesc recordType => PassStruct(recordType),
                EnumTypeDesc enumType => PassEnum(enumType),
                _ => throw UnsupportedJniScenarioException.UnsupportedType(type)
            };

            if (context is ValuePassContext.MethodParameter or ValuePassContext.StructFieldSetterParameter)
            {
                if (mainPass is null)
                {
                    throw new InvalidOperationException("Cannot have a void parameter.");
                }

                ParameterPasses.Add(mainPass);
                ParameterVariables.Add(mainVariable);
            }
            else
            {
                if (mainPass is not null and not StandaloneValuePass)
                {
                    throw new InvalidOperationException("A StandaloneValuePass is required in a return type.");
                }

                ReturnTypeVariable = mainVariable;
                ReturnTypePass = (StandaloneValuePass)mainPass;
            }

            ValuePass PassVoid()
            {
                mainVariable = VariableToGenerate.IdenticalJavaTypes("voidRet", JniType.Void, JavaType.Void);
                return null;
            }

            ValuePass PassString()
            {
                mainVariable = new VariableToGenerate(valueName,
                    JniType.JByteArray,
                    JavaType.ByteArray,
                    JavaType.String);

                if (context is ValuePassContext.MethodReturnValue or ValuePassContext.StructFieldSetterParameter)
                {
                    // In this scenario, we must know what to do with the returned
                    // const char* as some methods copy it with strdup, and some just don't.
                    // Therefore, we use FumoCement's StringDeletionBehaviour enum to know what to do with it.
                    // The enum has two members: DELETE and NO_DELETE.
                    //
                    // This enum will be converted as a bool in the public method, as it has only two members.
                    // "But why use an enum instead of a simple bool?" Because a using a bool would be confusing,
                    // instead, the enum clearly describes what the parameter is supposed to do.

                    var deletionEnumValueName = valueName + "DeletionBehaviour";

                    ParameterVariables.Add(new VariableToGenerate(deletionEnumValueName,
                        JniType.JBoolean,
                        JavaType.Boolean,
                        FumoCementTypes.StringDeletionBehaviour));

                    ParameterPasses.Add(new PassStringDeletionEnumAsBool(deletionEnumValueName));

                    return new PassStringAsJByteArrayToCharPtr(deletionEnumValueName, valueName);
                }

                return new PassStringAsJByteArrayToCharPtr(null, valueName);
            }

            ValuePass PassPointer(PointerTypeDesc pointerType)
            {
                mainVariable = VariableToGenerate.IdenticalJavaTypes(valueName,
                    JniType.JLong,
                    JavaType.Long.WithAddedAnnotations($"@Pointer(\"{pointerType.AsRawString}\")"));

                return new PassPointerAsJLongToPtr(pointerType, valueName);
            }

            ValuePass PassPrimitive(BuiltinTypeDesc builtinType)
            {
                // As per https://docs.oracle.com/javase/7/docs/technotes/guides/jni/spec/types.html
                // +-----------+-------------+------------------+
                // | Java Type | Native Type |   Description    |
                // +-----------+-------------+------------------+
                // | boolean   | jboolean    | unsigned 8 bits  |
                // | byte      | jbyte       | signed 8 bits    |
                // | char      | jchar       | unsigned 16 bits |
                // | short     | jshort      | signed 16 bits   |
                // | int       | jint        | signed 32 bits   |
                // | long      | jlong       | signed 64 bits   |
                // | float     | jfloat      | 32 bits          |
                // | double    | jdouble     | 64 bits          |
                // | void      | void        | N/A              |
                // +-----------+-------------+------------------+

                var jniType = builtinType.Kind switch {
                    CXTypeKind.CXType_Bool => JniType.JBoolean,

                    CXTypeKind.CXType_Char_U or
                        CXTypeKind.CXType_UChar or
                        CXTypeKind.CXType_SChar or
                        CXTypeKind.CXType_Char_S => JniType.JByte,

                    CXTypeKind.CXType_UShort or
                        CXTypeKind.CXType_Char16 => JniType.JChar,

                    CXTypeKind.CXType_Int or
                        CXTypeKind.CXType_UInt or
                        CXTypeKind.CXType_Char32 or
                        CXTypeKind.CXType_WChar => JniType.JInt,

                    CXTypeKind.CXType_Long or
                        CXTypeKind.CXType_ULong or
                        CXTypeKind.CXType_LongLong or
                        CXTypeKind.CXType_ULongLong => JniType.JLong,

                    CXTypeKind.CXType_Float => JniType.JFloat,
                    CXTypeKind.CXType_Double => JniType.JDouble,
                    _ => throw UnsupportedJniScenarioException.UnsupportedType(type)
                };

                var javaType = jniType.AsJavaNonObject();

                if (builtinType.Kind is
                    CXTypeKind.CXType_UInt or
                    CXTypeKind.CXType_Char32 or
                    CXTypeKind.CXType_Char_U or
                    CXTypeKind.CXType_Char_S or
                    CXTypeKind.CXType_WChar or
                    CXTypeKind.CXType_ULong)
                {
                    javaType = javaType.WithAddedAnnotations("@Unsigned");
                }

                mainVariable = VariableToGenerate.IdenticalJavaTypes(valueName, jniType, javaType);
                return new PassPrimitive(builtinType, valueName);
            }

            ValuePass PassStruct(RecordTypeDesc recordType)
            {
                var javaStructName = JniGenerationPlan.StructTypeName(recordType.Name);

                mainVariable = new VariableToGenerate(valueName,
                    JniType.JLong,
                    JavaType.Long.WithAddedAnnotations($"@Pointer(\"{recordType.Name}*\")"),
                    _generationPlan.NestedTypeInContainer(javaStructName));

                if (context is ValuePassContext.StructFieldReturnValue)
                {
                    return new PassNestedStructAsJLongPointerToStructPtr(recordType, javaStructName, valueName);
                }

                return new PassStructAsJLongPointerToStructCopy(recordType, javaStructName, valueName);
            }

            ValuePass PassEnum(EnumTypeDesc enumType)
            {
                const string MagicConstantAnnotation = "@org.intellij.lang.annotations.MagicConstant";

                var javaEnumName = JavaConventions.EscapeName(enumType.Name); // TODO: DRY this with a hairdryer
                var fullyQualifiedJavaEnumName = $"{_generationPlan.ContainerClass}.{javaEnumName}.class";

                mainVariable = VariableToGenerate.IdenticalJavaTypes(valueName,
                    JniType.JInt,
                    JavaType.Int
                        .WithAddedAnnotations(
                            $"{MagicConstantAnnotation}(flagsFromClass = {fullyQualifiedJavaEnumName})"));

                return new PassEnumValueAsValueTypeToEnum(enumType, valueName);
            }
        }

        public void ConsumeStructHandle()
        {
            var type = JavaType.Long.WithAddedAnnotations("@Pointer");

            ParameterPasses.Add(new PassStructHandleAsJLong());
            ParameterVariables.Add(new VariableToGenerate("handle",
                new JniType(JniType.JLong),
                type,
                null));
        }

        private void ConsumeFunctionPointerParameter(
            MethodParameter<TypeDesc> pointerParameter,
            string methodName)
        {
            var functionPointerType = (FunctionProtoTypeDesc)((PointerTypeDesc)pointerParameter.Type).PointeeType;
            var callbackInterface = $"Callback_{methodName}_{pointerParameter.Name}";
            var callbackInterfaceType = _generationPlan.NestedTypeInContainer(callbackInterface);

            // Let's add the callback interface and its callbackCall function
            var callbackGenerationSet = CreateCallbackGenSet();
            _generationPlan.JavaCallbacks.Add(
                new JavaCallbackGenerationInfo(callbackInterface, callbackGenerationSet));

            // Then add the function pointer parameter used to actually call Java from C.
            ParameterVariables.Add(new VariableToGenerate(pointerParameter.Name,
                JniType.JLong,
                JavaType.Long,
                FumoCementTypes.FunctionPointer(callbackInterface)));
            ParameterPasses.Add(new PassFunctionPointerAsContextPtr(pointerParameter.Name));

            // Native side now, this is the fun part!
            var callbackCallClass = callbackInterfaceType.FullJniClass;
            var callbackCallMethod = callbackGenerationSet.CallbackCallMethod.Name;
            var callbackCallSignature = callbackGenerationSet.CallbackCallMethod.JniSignature;

            ParameterPasses.Add(
                new PassFunctionPointerProxyLambda(pointerParameter.Name, CreateProxyGenSet(),
                    callbackCallClass, callbackCallMethod, callbackCallSignature));
            ParameterPasses.Add(new PassContextPtrAsVoidPtr(pointerParameter.Name));

            JavaCallbackGenerationSet CreateCallbackGenSet()
            {
                const string CallbackCall = "callbackCall";
                const string Execute = "execute";

                var generator = new ValuePassGenerator(_generationPlan);
                var parameters = functionPointerType.Parameters.SkipLast(1)
                    .Select((x, i) => new MethodParameter<TypeDesc>(x, $"param{i}"))
                    .ToList();

                generator.ParameterVariables.Add(new VariableToGenerate(pointerParameter.Name,
                    null, callbackInterfaceType, null)
                );

                generator.Consume(functionPointerType.ReturnType, "returnValue",
                    ValuePassContext.MethodReturnValue);
                generator.ConsumeFunctionParameters(parameters, CallbackCall);

                return new JavaCallbackGenerationSet(
                    new FullJavaMethod(CallbackCall,
                        generator.GeneratedReturnTypes.InternalJavaNative,
                        generator.GeneratedParameters.InternalJavaNative,
                        true),
                    new BodylessJavaMethod(Execute,
                        generator.GeneratedReturnTypes.PublicJava,
                        generator.GeneratedParameters.PublicJava,
                        false,
                        false),
                    generator.ReturnTypePass,
                    generator.ParameterPasses);
            }

            FunctionPointerProxyGenerationSet CreateProxyGenSet()
            {
                const string ContextParameter = "func$$rawContext";
                const string CastedContextVariable = "func$$actualContext";

                // This method will call the callbackCall method with the required values.
                var lastParamIndex = functionPointerType.Parameters.Count - 1;

                var generator = new ValuePassGenerator(_generationPlan);

                var nativeParameters = functionPointerType.Parameters
                    .Select((x, i) => new MethodParameter<TypeDesc>(x,
                        i == lastParamIndex ? ContextParameter : $"param{i}"))
                    .ToArray();

                // We won't pass the void* context to java!
                var passedParameters = nativeParameters.SkipLast(1).ToArray();

                generator.ParameterPasses.Add(new PassCallbackObject(CastedContextVariable));
                generator.Consume(functionPointerType.ReturnType, "returnValue",
                    ValuePassContext.MethodReturnValue);
                generator.ConsumeFunctionParameters(passedParameters, methodName);

                return new FunctionPointerProxyGenerationSet(
                    new NativeMethod("__lambda",
                        functionPointerType.ReturnType,
                        nativeParameters),
                    callbackGenerationSet.CallbackCallMethod,
                    generator.ReturnTypePass,
                    generator.ParameterPasses,
                    CastedContextVariable);
            }
        }
    }

    internal class VariableToGenerate
    {
        public static VariableToGenerate IdenticalJavaTypes(string name,
            JniType nativeType,
            JavaType internalJavaNative)
        {
            return new(name, nativeType, internalJavaNative, internalJavaNative);
        }

        public VariableToGenerate(string name, JniType? nativeType,
            JavaType internalJavaNative,
            JavaType publicJava)
        {
            NativeType = nativeType;
            InternalJavaNative = internalJavaNative;
            PublicJava = publicJava;
            Name = name;
        }

        public string Name { get; }

        public JniType? NativeType { get; }
        public JavaType InternalJavaNative { get; }
        public JavaType PublicJava { get; }
    }

    internal class GeneratedParameters
    {
        private readonly List<MethodParameter<JniType>> _native = new();
        private readonly List<MethodParameter<JavaType>> _internalJavaNative = new();
        private readonly List<MethodParameter<JavaType>> _publicJava = new();

        public IReadOnlyList<MethodParameter<JniType>> Native => _native;
        public IReadOnlyList<MethodParameter<JavaType>> InternalJavaNative => _internalJavaNative;
        public IReadOnlyList<MethodParameter<JavaType>> PublicJava => _publicJava;

        public GeneratedParameters(IEnumerable<VariableToGenerate> parameterVariables)
        {
            foreach (var variable in parameterVariables)
            {
                if (variable.NativeType is { } actualNativeType)
                {
                    _native.Add(new(actualNativeType, variable.Name));
                }

                if (variable.InternalJavaNative is { } actualInternalJavaNative)
                {
                    _internalJavaNative.Add(new(actualInternalJavaNative, variable.Name));
                }

                if (variable.PublicJava is { } actualPublicJava)
                {
                    _publicJava.Add(new(actualPublicJava, variable.Name));
                }
            }
        }
    }

    internal class GeneratedReturnTypes
    {
        public GeneratedReturnTypes(VariableToGenerate variable)
        {
            Native = variable?.NativeType ?? JniType.Void;
            InternalJavaNative = variable?.InternalJavaNative ?? JavaType.Void;
            PublicJava = variable?.PublicJava ?? JavaType.Void;
        }

        public JniType Native { get; }
        public JavaType InternalJavaNative { get; }
        public JavaType PublicJava { get; }
    }

    internal enum ValuePassContext
    {
        StructFieldReturnValue,
        StructFieldSetterParameter,
        MethodReturnValue,
        MethodParameter,
    }
}
