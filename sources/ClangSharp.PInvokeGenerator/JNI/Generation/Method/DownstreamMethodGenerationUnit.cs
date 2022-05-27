// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using ClangSharp.JNI.Generation.Transitions;
using ClangSharp.JNI.Java;

namespace ClangSharp.JNI.Generation.Method;

internal class DownstreamMethodGenerationUnit : MethodGenerationUnit
{
    public static DownstreamMethodGenerationUnit UseLinker(DownstreamMethodLinker linker,
        in DownstreamMethodGenerationProperties generationProperties,
        out ImmutableArray<TransformationUnit> generatedTransformationUnits)
    {
        var (operation, returnValueLinkage, parameterLinkages) = linker.Apply(out generatedTransformationUnits);

        return new DownstreamMethodGenerationUnit(operation, returnValueLinkage, parameterLinkages,
            generationProperties);
    }

    public FullJavaMethod JavaMethod { get; }
    public BodylessJavaMethod JavaNativeMethod { get; }
    public JniGlueMethod JniProxyMethod { get; }

    public DownstreamMethodGenerationUnit(FinalOperation finalOperation,
        MethodReturnValueLinkage? returnValueLinkage,
        IEnumerable<MethodParameterLinkage> parameterLinkages,
        in DownstreamMethodGenerationProperties generationProperties)
        : base(finalOperation, returnValueLinkage, parameterLinkages)
    {
        JavaMethod = new FullJavaMethod(
            generationProperties.JavaName,
            returnValueLinkage?.JavaType ?? JavaType.Void,
            FilterParameters(x => x.AsJavaParameter()),
            !generationProperties.IsJavaInstanceMethod);

        JavaNativeMethod = new BodylessJavaMethod(
            generationProperties.JniProxyName,
            returnValueLinkage?.JniType.AsJavaNonObject() ?? JavaType.Void,
            FilterParameters(x => x.AsJavaJniParameter()));

        JniProxyMethod = new JniGlueMethod(
            JavaConventions.JniProxyMethodName(generationProperties.ContainingType, generationProperties.JniProxyName),
            returnValueLinkage?.JniType ?? JniType.Void,
            FilterParameters(x => x.AsJniParameter()),
            generationProperties.ContainingType.Name);
    }
}

internal readonly record struct DownstreamMethodGenerationProperties(string JavaName, string JniProxyName,
    ObjectJavaType ContainingType,
    bool IsJavaInstanceMethod = false);
