// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ClangSharp.Abstractions;
using ClangSharp.Interop;
using ClangSharp.JNI.Generation.FunctionPointer;
using ClangSharp.JNI.Generation.Struct;
using ClangSharp.JNI.Generation.Transitions;
using ClangSharp.JNI.Java;

namespace ClangSharp.JNI.Generation.Method;

internal abstract class MethodLinker
{
    private readonly JniGenerationContext _context;
    private readonly List<TransformationUnit> _generatedTransformationUnits = new(0);
    private LinkedValue? _nextFunctionPointerContextLink = null;
    private NativeOperationParameter? _currentParameter = null;
    private readonly NativeOperation _operation;
    private readonly TransitionDirection _transitionDirection;

    private ValuePosition Position =>
        _currentParameter is null ? ValuePosition.ReturnType : ValuePosition.Parameter;

    public MethodLinker(NativeOperation operation, JniGenerationContext context,
        TransitionDirection transitionDirection)
    {
        _context = context;
        _transitionDirection = transitionDirection;
        _operation = operation;
    }

    public (NativeOperation operation, MethodReturnValueLinkage? returnValueLinkage,
        IReadOnlyList<MethodParameterLinkage> parameterLinkages) Apply(
            out ImmutableArray<TransformationUnit> generatedTransformationUnits)
    {
        var operation = _operation;
        var returnValueLinkage = LinkReturnValue();
        var parameterLinkages = _operation.Parameters.Select(LinkParameter).ToArray();
        generatedTransformationUnits = ImmutableArray.CreateRange(_generatedTransformationUnits);

        return (operation, returnValueLinkage, parameterLinkages);
    }

    public MethodReturnValueLinkage? LinkReturnValue()
    {
        if (_operation.ReturnType.Kind == CXTypeKind.CXType_Void)
        {
            return null;
        }

        _currentParameter = null;
        return LinkValue(_operation.ReturnType).AsReturnValue();
    }

    public MethodParameterLinkage LinkParameter(NativeOperationParameter parameter)
    {
        _currentParameter = parameter;

        // If we've already prepared a linked value for a void* context pointer
        // concerning a function pointer, use it.
        if (_nextFunctionPointerContextLink is not null)
        {
            var param = _nextFunctionPointerContextLink.AsParameter(parameter, _transitionDirection);
            _nextFunctionPointerContextLink = null;
            return param;
        }

        return parameter switch {
            StructHandleParameter structHandleParameter
                => LinkStructHandle(structHandleParameter).AsParameter(parameter, _transitionDirection),

            CallbackObjectParameter callbackObj
                => LinkUpstreamCallbackObject(callbackObj).AsParameter(parameter, _transitionDirection),

            _ => LinkValue(parameter.Type).AsParameter(parameter, _transitionDirection)
        };
    }

    private LinkedValue LinkValue(TypeDesc type)
    {
        return type switch {
            BuiltinTypeDesc builtinType => LinkPrimitive(builtinType),
            PointerTypeDesc { PointeeType.Kind: CXTypeKind.CXType_Char_S } stringType => LinkString(stringType),
            PointerTypeDesc { PointeeType: FunctionProtoTypeDesc functionPointerType }
                => LinkFunctionPointer(functionPointerType),
            PointerTypeDesc pointerType => LinkPointer(pointerType),
            RecordTypeDesc recordType => LinkStruct(recordType),
            EnumTypeDesc enumType => LinkEnum(enumType),
            _ => throw UnsupportedJniScenarioException.UnsupportedType(type)
        };
    }

    private static LinkedValue LinkPrimitive(BuiltinTypeDesc type)
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


        JniType jniType;
        bool unsignedAnnotation = false;
        // size_t, uintptr_t, etc.
        if (type.IsPointerLikeType(out var sizeTypeName))
        {
            // (We're assuming a 64-bit system.)
            // This is to alleviate an issue with headers giving unsigned int instead of unsigned long long
            // for size_t.
            jniType = JniType.JLong;
            if (sizeTypeName is not null && sizeTypeName.StartsWith('u'))
            {
                unsignedAnnotation = true;
            }
        }
        else if (type.IsFixedWidthIntegerType(out _, out var bits, out var fixedUnsigned))
        {
            unsignedAnnotation = fixedUnsigned && bits != 16; // jchar is a 16-bit unsigned type we can use
            jniType = (bits, fixedUnsigned) switch {
                (8, _) => JniType.JByte,
                (16, false) => JniType.JShort,
                (16, true) => JniType.JChar,
                (32, _) => JniType.JInt,
                (64, _) => JniType.JLong,
                _ => throw new InvalidOperationException()
            };
        }
        else
        {
            jniType = type.Kind switch {
                CXTypeKind.CXType_Bool => JniType.JBoolean,

                CXTypeKind.CXType_Char_U or
                    CXTypeKind.CXType_UChar or
                    CXTypeKind.CXType_SChar or
                    CXTypeKind.CXType_Char_S => JniType.JByte,

                CXTypeKind.CXType_Short => JniType.JShort,

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
        }

        unsignedAnnotation = unsignedAnnotation ||
                   type.Kind is CXTypeKind.CXType_UInt or
                       CXTypeKind.CXType_Char32 or
                       CXTypeKind.CXType_Char_U or
                       CXTypeKind.CXType_Char_S or
                       CXTypeKind.CXType_WChar or
                       CXTypeKind.CXType_ULong;

        var javaType = jniType.AsJavaNonObject();
        if (unsignedAnnotation)
        {
            javaType = javaType.WithAddedAnnotations("@Unsigned");
        }

        return new LinkedValue(new PrimitiveValueTransition()) {
            JavaType = javaType, JniType = jniType, NativeType = type
        };
    }

    private LinkedValue LinkString(PointerTypeDesc type)
    {
        TransitingMethodParameter? stringDeletionParameter = null;
        var isStructSetter = _operation is SetStructFieldOperation;

        // Method return type or struct setter
        if (isStructSetter ? Position is ValuePosition.Parameter : Position is ValuePosition.ReturnType)
        {
            // In this scenario, we must know what to do with the returned
            // const char* as some methods copy it with strdup, and some just don't.
            // Therefore, we use FumoCement's StringDeletionBehaviour enum to know what to do with it.
            // The enum has two members: DELETE and NO_DELETE.
            //
            // This enum will be converted as a bool in the public method, as it has only two members.
            // "But why use an enum instead of a simple bool?" Because a using a bool would be confusing,
            // instead, the enum clearly describes what the parameter is supposed to do.

            var parameterName = Position is ValuePosition.Parameter
                ? "overwrittenValueDeletionBehaviour"
                : "returnValueDeletionBehaviour";

            stringDeletionParameter = new TransitingMethodParameter(parameterName,
                javaType: WellKnownJniTypes.Java.StringDeletionBehaviour,
                jniType: JniType.JBoolean,
                nativeType: null,
                new StringDeletionEnumValueTransition(),
                _transitionDirection,
                isExceptionalParameter: true);
        }

        var value = new LinkedValue(new CharPointerStringValueTransition(stringDeletionParameter?.Name)) {
            JavaType = JavaType.String,
            JniType = JniType.JByteArray,
            NativeType = type,
            ExtraGeneratedParameters = stringDeletionParameter is not null
                ? ImmutableArray.Create(stringDeletionParameter)
                : ImmutableArray<TransitingMethodParameter>.Empty
        };
        return value;
    }

    private static LinkedValue LinkPointer(PointerTypeDesc type)
    {
        var annotation = $"@Pointer(\"{type.AsVerbatimString}\")";

        return new LinkedValue(new PointerValueTransition(type)) {
            JavaType = JavaType.Long.WithAddedAnnotations(annotation), JniType = JniType.JLong, NativeType = type
        };
    }

    private LinkedValue LinkStruct(RecordTypeDesc type)
    {
        var structTransformation = _context
            .GetTransformationUnits<StructTransformationUnit>()
            .FirstOrDefault(x => x.Target.NativeName == type.Name);

        var javaStructType = structTransformation?.ClassGenerationUnit.JavaStructType;

        TransitionAction transitionAction;
        var isNestedStruct = _operation is GetStructFieldOperation or SetStructFieldOperation;
        if (isNestedStruct)
        {
            transitionAction = new NestedStructRefValueTransition(javaStructType!.Name, type);
        }
        else
        {
            transitionAction = new StructCopyValueTransition(javaStructType?.Name, type);
        }

        return new LinkedValue(transitionAction) {
            JavaType = (JavaType?)javaStructType ?? JavaType.Long.WithAddedAnnotations($"@Pointer(\"{type.Name}*\")"),
            JniType = JniType.JLong,
            NativeType = type
        };
    }

    private LinkedValue LinkEnum(EnumTypeDesc type)
    {
        const string MagicConstantAnnotation = "@org.intellij.lang.annotations.MagicConstant";

        var javaEnumName = JavaConventions.EscapeName(type.Name);
        var annotation = $"{MagicConstantAnnotation}(flagsFromClass = {_context.Package}.{javaEnumName}.class)";

        return new LinkedValue(new EnumValueTransition(type)) {
            JavaType = JavaType.Int.WithAddedAnnotations(annotation), JniType = JniType.JInt, NativeType = type
        };
    }

    private LinkedValue LinkFunctionPointer(FunctionProtoTypeDesc type)
    {
        if (_currentParameter is null)
        {
            throw new UnsupportedJniScenarioException(
                "Passing function pointers by return type is not yet supported.");
        }

        if (type.Parameters[^1] is not PointerTypeDesc { PointeeType.Kind: CXTypeKind.CXType_Void })
        {
            throw new UnsupportedJniScenarioException(
                $"The function pointer {type} does not have a void* context parameter.");
        }

        var nextParamIndex = _operation.Parameters.IndexOf(_currentParameter) + 1;
        if (_operation.Parameters.ElementAtOrDefault(nextParamIndex) is not {
                Type: PointerTypeDesc { PointeeType.Kind: CXTypeKind.CXType_Void }
            })
        {
            throw new UnsupportedJniScenarioException(
                $"The function pointer parameter '{_currentParameter.Name}' does not have a leading void* context parameter.");
        }

        var target = FunctionPointerTarget.FromParentNativeOperation(_operation, _currentParameter);

        var functionPointerUnit =
            new FunctionPointerTransformationUnit(target, _context, out var generatedTransformationUnits);

        _generatedTransformationUnits.Add(functionPointerUnit);
        _generatedTransformationUnits.AddRange(generatedTransformationUnits);

        var callbackLambdaLink = new LinkedValue(new CallbackCallerLambdaTransition(functionPointerUnit)) {
            TransitionBehaviors = new TransitionBehaviorSet { JniToNative = TransitionBehavior.Generate }
        };
        var contextLink = new LinkedValue(new CallbackObjectTransition()) {
            JavaType = WellKnownJniTypes.Java.FunctionPointer(functionPointerUnit.InterfaceGenerationUnit.Name),
            JniType = JniType.JLong,
            NativeType = PointerTypeDesc.VoidPointer,
        };

        _nextFunctionPointerContextLink = contextLink;
        return callbackLambdaLink;
    }

    private static LinkedValue LinkStructHandle(StructHandleParameter structHandleParameter)
    {
        return new LinkedValue(new CurrentStructHandleValueTransition(structHandleParameter.StructType)) {
            JniType = JniType.JLong,
            NativeType = structHandleParameter.Type,
            IntermediateName = "struct$ptr",
            TransitionBehaviors = new TransitionBehaviorSet {
                JavaToJni = TransitionBehavior.Generate, JniToNative = TransitionBehavior.Transit
            }
        };
    }

    private static LinkedValue LinkUpstreamCallbackObject(CallbackObjectParameter callbackObjectParameter)
    {
        return new LinkedValue(new CallbackObjectTransition()) {
            JavaType = callbackObjectParameter.JavaCallbackType,
            JavaJniType = callbackObjectParameter.JavaCallbackType,
            JniType = JniType.JObject,
            NativeType = PointerTypeDesc.VoidPointer,
            IsJavaReceiver = true
        };
    }

    private record LinkedValue(TransitionAction TransitionAction)
    {
        public JavaType? JavaType { get; init; } = null;
        public JniType? JniType { get; init; } = null;
        public JavaType? JavaJniType { get; init; } = null;
        public TypeDesc? NativeType { get; init; } = null;

        public string? IntermediateName { get; init; } = null;
        public int IntermediateOrdering { get; init; } = 0;

        public TransitionBehaviorSet? TransitionBehaviors { get; init; } = null;
        public bool IsExceptionalParameter { get; init; } = false;

        public bool IsJavaReceiver { get; init; } = false;

        public ImmutableArray<TransitingMethodParameter> ExtraGeneratedParameters { get; init; }
            = ImmutableArray<TransitingMethodParameter>.Empty;

        public MethodReturnValueLinkage AsReturnValue()
        {
            if (JavaType is null || JniType is not { } validJniType || NativeType is null)
            {
                throw new InvalidOperationException("Cannot partially generate a return value.");
            }

            return new MethodReturnValueLinkage(JavaType, validJniType, NativeType, TransitionAction,
                ExtraGeneratedParameters, JavaJniType);
        }

        public MethodParameterLinkage AsParameter(NativeOperationParameter parameter, TransitionDirection direction)
        {
            var newParameter = new TransitingMethodParameter(parameter.Name, JavaType, JniType, NativeType,
                TransitionAction, direction, JavaJniType, IntermediateName, TransitionBehaviors,
                IsExceptionalParameter, IntermediateOrdering, IsJavaReceiver);

            return new MethodParameterLinkage(parameter, newParameter, ExtraGeneratedParameters);
        }
    }

    private enum ValuePosition
    {
        ReturnType,
        Parameter
    }
}

internal sealed class DownstreamMethodLinker : MethodLinker
{
    public DownstreamMethodLinker(NativeOperation operation, JniGenerationContext context)
        : base(operation, context, TransitionDirection.Downstream)
    {
    }
}

internal sealed class UpstreamMethodLinker : MethodLinker
{
    public UpstreamMethodLinker(NativeOperation operation, JniGenerationContext context)
        : base(operation, context, TransitionDirection.Upstream)
    {
    }
}
