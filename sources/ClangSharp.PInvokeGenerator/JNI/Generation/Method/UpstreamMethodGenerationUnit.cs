// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ClangSharp.Abstractions;
using ClangSharp.JNI.Generation.Configuration;
using ClangSharp.JNI.Generation.FunctionPointer;
using ClangSharp.JNI.Java;
using ClangSharp.JNI.JNIGlue;

#nullable enable
namespace ClangSharp.JNI.Generation.Method;

internal class UpstreamMethodGenerationUnit : MethodGenerationUnit
{
    public static UpstreamMethodGenerationUnit UseLinker(UpstreamMethodLinker linker,
        UpstreamMethodGenerationProperties generationProperties,
        out ImmutableArray<TransformationUnit> generatedTransformationUnits)
    {
        var (operation, returnValueLinkage, parameterLinkages) = linker.Apply(out generatedTransformationUnits);

        return new UpstreamMethodGenerationUnit(operation, returnValueLinkage, parameterLinkages,
            generationProperties);
    }

    public UpstreamMethodGenerationUnit(NativeOperation nativeOperation,
        MethodReturnValueLinkage? returnValueLinkage,
        IEnumerable<MethodParameterLinkage> parameterLinkages,
        UpstreamMethodGenerationProperties methodGenerationProperties)
        : base(nativeOperation, returnValueLinkage, parameterLinkages)
    {
        JniCallbackCallerLambda = new NativeMethod(
            "*lambda_function*",
            returnValueLinkage?.NativeType ?? BuiltinTypeDesc.Void,
            FilterParameters(x => x.AsNativeParameter()));

        CallbackCallerMethod = new FullJavaMethod(
            methodGenerationProperties.Namings.CallbackInterfaceCallerMethod,
            returnValueLinkage?.JniType.AsJavaNonObject() ?? JavaType.Void,
            FilterParameters(x => x.AsJavaJniParameter()),
            isStatic: true);

        CallbackMethod = new BodylessJavaMethod(
            methodGenerationProperties.Namings.CallbackInterfaceMethod,
            returnValueLinkage?.JavaType ?? JavaType.Void,
            FilterParameters(x => x.AsJavaParameter(nullsIfReceiver: true)),
            "public",
            isNative: false,
            isStatic: false);

        CallbackType = methodGenerationProperties.CallbackType;
        CallbackObjectParameter = FindCallbackObjectParameter();
    }

    public NativeMethod JniCallbackCallerLambda { get; }
    public FullJavaMethod CallbackCallerMethod { get; }
    public BodylessJavaMethod CallbackMethod { get; }

    public ObjectJavaType CallbackType { get; }

    public TransitingMethodParameter CallbackObjectParameter { get; }

    private TransitingMethodParameter FindCallbackObjectParameter()
    {
        return ParameterLinkages.First(x => x.TargetParameter is CallbackObjectParameter).TransitingParameter;
    }
}

internal readonly record struct UpstreamMethodGenerationProperties(ObjectJavaType CallbackType,
    JniNamings Namings);
