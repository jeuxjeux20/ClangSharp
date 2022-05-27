// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

#nullable enable
using System.Collections.Generic;
using System.Linq;
using ClangSharp.Abstractions;
using ClangSharp.JNI.Generation.Transitions;
using ClangSharp.JNI.Java;

namespace ClangSharp.JNI.Generation.Method;

internal record MethodReturnValueLinkage
{
    public JavaType JavaType { get; }
    public JniType JniType { get; }
    public JavaType JavaJniType { get; }
    public TypeDesc NativeType { get; }
    public TransitionAction TransitionAction { get; }

    public IReadOnlyList<TransitingMethodParameter> GeneratedParameters { get; }

    public GeneratedExpression TransitValue(string expression,
        TransitionKind transitionKind, MethodGenerationUnit generationUnit)
        => TransitionAction.TransitValue(expression, transitionKind, generationUnit);

    public MethodReturnValueLinkage(JavaType javaType, JniType jniType, TypeDesc nativeType,
        TransitionAction transitionAction, IEnumerable<TransitingMethodParameter> generatedParameters,
        JavaType? javaJniType = null)
    {
        JavaType = javaType;
        JniType = jniType;
        NativeType = nativeType;
        JavaJniType = javaJniType ?? JniType.AsJavaNonObject();
        TransitionAction = transitionAction;
        GeneratedParameters = generatedParameters.ToArray();
    }
}
