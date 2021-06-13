// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using ClangSharp.Abstractions;
using ClangSharp.JNI.Java;

namespace ClangSharp.JNI
{
    internal sealed class JniGenerationPlan
    {
        public string ContainerClass { get; set; } = "Native";

        public string Package { get; set; }

        public List<MethodGenerationInfo> Methods { get; } = new();

        public List<StructGenerationInfo> Structs { get; } = new();

        public List<EnumGenerationInfo> Enums { get; } = new();

        public List<JavaCallbackGenerationInfo> JavaCallbacks { get; } = new();

        public StructGenerationInfo MakeStructGenerationInfo(string structName, IEnumerable<StructFieldGenerationInfo> fields)
            => new(structName, StructTypeName(structName), StructTypeInContainer(structName), fields);

        public ObjectJavaType NestedTypeInContainer(string name)
            => new(Package, $"{ContainerClass}.{name}");

        public ObjectJavaType StructTypeInContainer(string name)
            => new(Package, $"{ContainerClass}.{StructTypeName(name)}");

        public static string StructTypeName(string name)
            => $"{name}";
    }

    internal sealed class StructGenerationInfo
    {
        public StructGenerationInfo(string nativeName, string javaName, ObjectJavaType javaType, IEnumerable<StructFieldGenerationInfo> fields)
        {
            NativeName = nativeName;
            JavaType = javaType;
            JavaName = javaName;
            Fields = fields.ToArray();
        }

        public string NativeName { get; }
        public string JavaName { get; }

        public ObjectJavaType JavaType { get; }

        public IReadOnlyList<StructFieldGenerationInfo> Fields { get; }

        public string AllocateStructMethodName => "allocateStruct";
        public string DestroyStructMethodName => "destroyStruct";
    }

    internal sealed class StructFieldGenerationInfo
    {
        public StructFieldGenerationInfo(string name,
            TypeDesc type,
            in MethodGenerationSet getterGenerationSet,
            in MethodGenerationSet setterGenerationSet)
        {
            Name = name;
            Type = type;

            GetterGenerationSet = getterGenerationSet;
            SetterGenerationSet = setterGenerationSet;
        }

        public string Name { get; }
        public TypeDesc Type { get; }

        public MethodGenerationSet GetterGenerationSet { get; }
        public MethodGenerationSet SetterGenerationSet { get; }
    }

    internal sealed class MethodGenerationInfo
    {
        public MethodGenerationInfo(NativeMethod nativeMethod, in MethodGenerationSet generationSet)
        {
            NativeMethod = nativeMethod;
            GenerationSet = generationSet;
        }

        public NativeMethod NativeMethod { get; }
        public MethodGenerationSet GenerationSet { get; }
    }

    internal sealed class EnumGenerationInfo
    {
        public string JavaName { get; }
        public IReadOnlyList<EnumConstantGenerationInfo> Constants { get; }

        public EnumGenerationInfo(string javaName, IEnumerable<EnumConstantGenerationInfo> constants)
        {
            JavaName = javaName;
            Constants = constants.ToArray();
        }
    }

    internal readonly struct EnumConstantGenerationInfo
    {
        public static EnumConstantGenerationInfo Unsigned(string name, ulong? unsignedValue)
        {
            return new(name, unsignedValue, null);
        }

        public static EnumConstantGenerationInfo Signed(string name, long? signedValue)
        {
            return new(name, null, signedValue);
        }

        public string JavaName { get; }
        public ulong? UnsignedValue { get; }
        public long? SignedValue { get; }

        public bool IsSigned => SignedValue is not null;
        public bool IsUnsigned => UnsignedValue is not null;

        private EnumConstantGenerationInfo(string javaName, ulong? unsignedValue, long? signedValue)
        {
            JavaName = javaName;
            UnsignedValue = unsignedValue;
            SignedValue = signedValue;
        }
    }

    internal sealed class JavaCallbackGenerationInfo
    {
        public JavaCallbackGenerationInfo(string callbackInterface,
            JavaCallbackGenerationSet callbackGenerationSet)
        {
            CallbackInterface = callbackInterface;
            CallbackGenerationSet = callbackGenerationSet;
        }

        public string CallbackInterface { get; }

        public JavaCallbackGenerationSet CallbackGenerationSet { get; }
    }

    internal sealed class JavaCallbackGenerationSet
    {
        public JavaCallbackGenerationSet(FullJavaMethod callbackCallMethod,
            BodylessJavaMethod upstreamInterfaceMethod,
            StandaloneValuePass upstreamReturnValuePass,
            IEnumerable<ValuePass> upstreamParameterPasses)
        {
            CallbackCallMethod = callbackCallMethod;
            UpstreamInterfaceMethod = upstreamInterfaceMethod;
            if (upstreamReturnValuePass is not null &&
                (upstreamReturnValuePass.Layers & ValuePassLayers.JavaToJni) != 0)
            {
                UpstreamReturnValuePass = upstreamReturnValuePass;
            }

            UpstreamParameterPasses = upstreamParameterPasses
                .Where(x => (x.Layers & ValuePassLayers.JavaToJni) != 0)
                .ToArray();
        }

        public FullJavaMethod CallbackCallMethod { get; }
        public BodylessJavaMethod UpstreamInterfaceMethod { get; }

        public StandaloneValuePass UpstreamReturnValuePass { get; }
        public IReadOnlyList<ValuePass> UpstreamParameterPasses { get; }
    }

    /// <summary>
    /// A set of data used to generate a function pointer proxy lambda.
    /// </summary>
    internal sealed class FunctionPointerProxyGenerationSet
    {
        public FunctionPointerProxyGenerationSet(NativeMethod nativeLambda,
            FullJavaMethod upstreamCallbackCallMethod,
            StandaloneValuePass upstreamReturnValuePass,
            IEnumerable<ValuePass> upstreamParameterPasses, string castedFuncContextVariable)
        {
            NativeLambda = nativeLambda;
            UpstreamCallbackCallMethod = upstreamCallbackCallMethod;
            CastedFuncContextVariable = castedFuncContextVariable;
            if (upstreamReturnValuePass is not null &&
                (upstreamReturnValuePass.Layers & ValuePassLayers.JavaToJni) != 0)
            {
                UpstreamReturnValuePass = upstreamReturnValuePass;
            }

            UpstreamParameterPasses = upstreamParameterPasses
                .Where(x => (x.Layers & ValuePassLayers.JavaToJni) != 0)
                .ToArray();
        }

        public NativeMethod NativeLambda { get; }
        public FullJavaMethod UpstreamCallbackCallMethod { get; }

        public string CastedFuncContextVariable { get; }

        public StandaloneValuePass UpstreamReturnValuePass { get; }
        public IReadOnlyList<ValuePass> UpstreamParameterPasses { get; }
    }

    internal sealed class MethodGenerationSet
    {
        public MethodGenerationSet(JniGlueMethod jniGlueMethod,
            BodylessJavaMethod internalJavaNativeMethod,
            FullJavaMethod publicJavaMethod,
            StandaloneValuePass returnValuePass,
            IEnumerable<ValuePass> parameterPasses)
        {
            JniGlueMethod = jniGlueMethod;
            InternalJavaNativeMethod = internalJavaNativeMethod;
            PublicJavaMethod = publicJavaMethod;

            if (returnValuePass is not null && (returnValuePass.Layers & ValuePassLayers.JniToC) != 0)
            {
                JniToCReturnValuePass = returnValuePass;
            }

            if (returnValuePass is not null && (returnValuePass.Layers & ValuePassLayers.JavaToJni) != 0)
            {
                JavaToJniReturnValuePass = returnValuePass;
            }

            JniToCParameterPasses = parameterPasses
                .Where(x => (x.Layers & ValuePassLayers.JniToC) != 0)
                .ToArray();
            JavaToJniParameterPasses = parameterPasses
                .Where(x => (x.Layers & ValuePassLayers.JavaToJni) != 0)
                .ToArray();
        }

        public JniGlueMethod JniGlueMethod { get; }
        public BodylessJavaMethod InternalJavaNativeMethod { get; }
        public FullJavaMethod PublicJavaMethod { get; }

        public StandaloneValuePass JniToCReturnValuePass { get; }
        public StandaloneValuePass JavaToJniReturnValuePass { get; }
        public IReadOnlyList<ValuePass> JniToCParameterPasses { get; }
        public IReadOnlyList<ValuePass> JavaToJniParameterPasses { get; }
    }

    internal abstract class Method<T>
    {
        public Method(string name, T returnType, IEnumerable<MethodParameter<T>> parameters)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = parameters.ToArray();
        }

        public string Name { get; }
        public T ReturnType { get; }
        public IReadOnlyList<MethodParameter<T>> Parameters { get; }
    }

    internal sealed class NativeMethod : Method<TypeDesc>
    {
        public NativeMethod(string name,
            TypeDesc returnType,
            IEnumerable<MethodParameter<TypeDesc>> parameters) : base(name, returnType, parameters)
        {
        }
    }

    internal sealed class JniGlueMethod : Method<JniType>
    {
        public JniGlueMethod(string name,
            JniType returnType,
            IEnumerable<MethodParameter<JniType>> parameters,
            string containingType) : base(name, returnType, parameters)
        {
            ContainingType = containingType;
        }

        public string ContainingType { get; }
    }

    internal sealed class BodylessJavaMethod : Method<JavaType>
    {
        public BodylessJavaMethod(string name,
            JavaType returnType,
            IEnumerable<MethodParameter<JavaType>> parameters,
            bool isNative = true,
            bool isStatic = true) : base(name, returnType, parameters)
        {
            IsNative = isNative;
            IsStatic = isStatic;
        }

        public bool IsNative { get; }
        public bool IsStatic { get; }
    }

    internal sealed class FullJavaMethod : Method<JavaType>
    {
        public FullJavaMethod(string name,
            JavaType returnType,
            IEnumerable<MethodParameter<JavaType>> parameters,
            bool isStatic) : base(name, returnType, parameters)
        {
            IsStatic = isStatic;
            JniSignature = MakeJniTypeSignature();
        }

        private string MakeJniTypeSignature()
        {
            var parameterSigs = string.Join("", Parameters.Select(x => x.Type.JniTypeSignature));
            var returnSig = ReturnType.JniTypeSignature;

            return $"({parameterSigs}){returnSig}";
        }

        public bool IsStatic { get; }

        public string JniSignature { get; }
    }

    [Flags]
    public enum ValuePassLayers
    {
        JniToC = 1 << 0,
        JavaToJni = 1 << 1,

        All = JniToC | JavaToJni
    }

    /// <summary>
    /// Defines how to pass a value to C, JNI or Java.
    /// </summary>
    internal abstract class ValuePass
    {
        public virtual ValuePassLayers Layers => ValuePassLayers.All;

        public string IntermediateVariableName => VariableNameHint + "$$intermediate";

        protected abstract string VariableNameHint { get; }
    }

    /// <summary>
    /// A <see cref="ValuePass"/> which is composed from one variable and does not require any external
    /// state to be valid.
    /// </summary>
    internal abstract class StandaloneValuePass : ValuePass
    {
        public string ValueToPass { get; }

        protected override string VariableNameHint => ValueToPass;

        public StandaloneValuePass(string valueToPass)
        {
            ValueToPass = valueToPass;
        }
    }

    internal sealed class PassStructHandleAsJLong : ValuePass
    {
        public override ValuePassLayers Layers => ValuePassLayers.JavaToJni;

        protected override string VariableNameHint => "handle";
    }

    internal sealed class PassStringAsJByteArrayToCharPtr : StandaloneValuePass
    {
        public bool RequiresDeletionEnum => DeletionEnumParameter != null;
        public string DeletionEnumParameter { get; }

        public PassStringAsJByteArrayToCharPtr(string deletionEnumParameter, string valueToPass) : base(valueToPass)
        {
            DeletionEnumParameter = deletionEnumParameter;
        }
    }

    internal sealed class PassStringDeletionEnumAsBool : StandaloneValuePass
    {
        public override ValuePassLayers Layers => ValuePassLayers.JavaToJni;

        public PassStringDeletionEnumAsBool(string valueToPass) : base(valueToPass)
        {
        }
    }

    internal sealed class PassNestedStructAsJLongPointerToStructPtr : StandaloneValuePass
    {
        public PassNestedStructAsJLongPointerToStructPtr(RecordTypeDesc record, string javaStructName,
            string valueToPass) : base(valueToPass)
        {
            Record = record;
            JavaStructName = javaStructName;
        }

        public RecordTypeDesc Record { get; }
        public string JavaStructName { get; }
    }

    internal sealed class PassStructAsJLongPointerToStructCopy : StandaloneValuePass
    {
        public PassStructAsJLongPointerToStructCopy(RecordTypeDesc record, string javaStructName,
            string valueToPass) : base(valueToPass)
        {
            Record = record;
            JavaStructName = javaStructName;
        }

        public RecordTypeDesc Record { get; }
        public string JavaStructName { get; }
    }

    internal sealed class PassPointerAsJLongToPtr : StandaloneValuePass
    {
        public PassPointerAsJLongToPtr(PointerTypeDesc pointer, string valueToPass) : base(valueToPass)
        {
            Pointer = pointer;
        }

        public PointerTypeDesc Pointer { get; }
    }

    internal sealed class PassEnumLongAsJLongToEnum : StandaloneValuePass
    {
        public EnumTypeDesc EnumType { get; }

        public PassEnumLongAsJLongToEnum(EnumTypeDesc enumType, string valueToPass) : base(valueToPass)
        {
            EnumType = enumType;
        }
    }

    internal sealed class PassFunctionPointerAsContextPtr : StandaloneValuePass
    {
        public PassFunctionPointerAsContextPtr(string valueToPass) : base(valueToPass)
        {
        }

        public override ValuePassLayers Layers => ValuePassLayers.JavaToJni;
    }

    internal sealed class PassContextPtrAsVoidPtr : ValuePass
    {
        public string JavaFunctionHandleVariable { get; }

        public PassContextPtrAsVoidPtr(string javaFunctionHandleVariable)
        {
            JavaFunctionHandleVariable = javaFunctionHandleVariable;
        }

        public override ValuePassLayers Layers => ValuePassLayers.JniToC;
        protected override string VariableNameHint => JavaFunctionHandleVariable + "$$context";
    }

    internal sealed class PassFunctionPointerProxyLambda : StandaloneValuePass
    {
        public FunctionPointerProxyGenerationSet LambdaProxyGenerationSet { get; }

        public string CallbackCallClass { get; }
        public string CallbackCallMethod { get; }
        public string CallbackCallSignature { get; }

        public PassFunctionPointerProxyLambda(string valueToPass,
            FunctionPointerProxyGenerationSet lambdaProxyGenerationSet,
            string callbackCallClass, string callbackCallMethod, string callbackCallSignature) : base(valueToPass)
        {
            LambdaProxyGenerationSet = lambdaProxyGenerationSet;
            CallbackCallClass = callbackCallClass;
            CallbackCallMethod = callbackCallMethod;
            CallbackCallSignature = callbackCallSignature;
        }

        public override ValuePassLayers Layers => ValuePassLayers.JniToC;
    }

    internal sealed class PassCallbackObject : ValuePass
    {
        public PassCallbackObject(string contextVariable)
        {
            ContextVariable = contextVariable;
        }

        public string ContextVariable { get; }

        protected override string VariableNameHint => "callback";

        public override ValuePassLayers Layers => ValuePassLayers.JavaToJni;
    }

    internal sealed class PassPrimitive : StandaloneValuePass
    {
        public PassPrimitive(BuiltinTypeDesc builtin, string valueToPass) : base(valueToPass)
        {
            Builtin = builtin;
        }

        public BuiltinTypeDesc Builtin { get; }
    }

    internal readonly struct MethodParameter<T>
    {
        public MethodParameter(T type, string name)
        {
            Type = type;
            Name = name;
        }

        public T Type { get; }
        public string Name { get; }

        public override string ToString() => $"{Type} {Name}";
    }


    internal readonly struct JniType : IEquatable<JniType>
    {
        public static readonly JniType Void = new("void");

        public static readonly JniType JObject = new("jobject");
        public static readonly JniType JClass = new("jclass");
        public static readonly JniType JString = new("jstring");

        public static readonly JniType JBoolean = new("jboolean");
        public static readonly JniType JByte = new("jbyte");
        public static readonly JniType JChar = new("jchar");
        public static readonly JniType JShort = new("jshort");
        public static readonly JniType JInt = new("jint");
        public static readonly JniType JLong = new("jlong");
        public static readonly JniType JFloat = new("jfloat");
        public static readonly JniType JDouble = new("jdouble");

        public static readonly JniType JArray = new("jarray");
        public static readonly JniType JObjectArray = new("jobjectArray");
        public static readonly JniType JBooleanArray = new("jbooleanArray");
        public static readonly JniType JByteArray = new("jbyteArray");
        public static readonly JniType JCharArray = new("jcharArray");
        public static readonly JniType JShortArray = new("jshortArray");
        public static readonly JniType JIntArray = new("jintArray");
        public static readonly JniType JLongArray = new("jlongArray");
        public static readonly JniType JFloatArray = new("jfloatArray");
        public static readonly JniType JDoubleArray = new("jdoubleArray");

        public static readonly JniType JThrowable = new("jthrowable");

        public JniType(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static implicit operator string(JniType type) => type.Value;

        public override string ToString() => Value;

        private static readonly Dictionary<JniType, JavaType> s_jniToJava = new() {
            [Void] = JavaType.Void,
            [JBoolean] = JavaType.Boolean,
            [JByte] = JavaType.Byte,
            [JChar] = JavaType.Char,
            [JShort] = JavaType.Short,
            [JInt] = JavaType.Int,
            [JLong] = JavaType.Long,
            [JFloat] = JavaType.Float,
            [JDouble] = JavaType.Double,

            [JObjectArray] = JavaType.ObjectArray,
            [JBooleanArray] = JavaType.BooleanArray,
            [JByteArray] = JavaType.ByteArray,
            [JCharArray] = JavaType.CharArray,
            [JShortArray] = JavaType.ShortArray,
            [JIntArray] = JavaType.IntArray,
            [JLongArray] = JavaType.LongArray,
            [JFloatArray] = JavaType.FloatArray,
            [JDoubleArray] = JavaType.DoubleArray,

            [JClass] = JavaType.Class,
            [JString] = JavaType.String,
        };
        public JavaType AsJavaNonObject()
        {
            if (!s_jniToJava.TryGetValue(this, out var javaType))
            {
                throw new InvalidOperationException($"Couldn't convert JNI (non-object) type to Java: '{Value}'.");
            }

            return javaType;
        }

        public bool Equals(JniType other)
            => string.Equals(Value, other.Value, StringComparison.InvariantCulture);

        public override bool Equals(object obj) => obj is JniType other && Equals(other);

        public override int GetHashCode() => (Value != null ? StringComparer.InvariantCulture.GetHashCode(Value) : 0);

        public static bool operator ==(JniType left, JniType right) => left.Equals(right);

        public static bool operator !=(JniType left, JniType right) => !left.Equals(right);
    }
}
