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
    public TypeDesc NativeType { get; }
    public ValueTransition TransitionBehavior { get; }

    public IReadOnlyList<TransitingMethodParameter> GeneratedParameters { get; }

    public GeneratedExpression TransitValue(string expression,
        TransitionKind transitionKind, MethodGenerationUnit generationUnit)
        => TransitionBehavior.TransitValue(expression, transitionKind, generationUnit);

    public MethodReturnValueLinkage(JavaType javaType, JniType jniType, TypeDesc nativeType,
        ValueTransition transitionBehavior, IEnumerable<TransitingMethodParameter> generatedParameters)
    {
        JavaType = javaType;
        JniType = jniType;
        NativeType = nativeType;
        TransitionBehavior = transitionBehavior;
        GeneratedParameters = generatedParameters.ToArray();
    }
}
