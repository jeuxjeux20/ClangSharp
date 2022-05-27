// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

#nullable enable
using System;
using System.Collections.Immutable;
using ClangSharp.Abstractions;
using ClangSharp.JNI.Generation.Transitions;
using ClangSharp.JNI.Java;

namespace ClangSharp.JNI.Generation.Method;

internal record TransitingMethodParameter
{
    public TransitingMethodParameter(
        string name,
        JavaType? javaType,
        JniType? jniType,
        TypeDesc? nativeType,
        TransitionAction transitionAction,
        TransitionDirection transitionDirection,
        JavaType? javaJniType = null,
        string? intermediateName = null,
        TransitionBehaviorSet? transitionBehaviors = null,
        bool isExceptionalParameter = false)
    {
        Name = name;
        JavaType = javaType;
        JavaJniType = javaJniType ?? jniType?.AsJavaNonObject();
        JniType = jniType;
        NativeType = nativeType;
        TransitionAction = transitionAction;
        TransitionDirection = transitionDirection;
        IntermediateName = intermediateName ?? name + "$int";

        if (transitionBehaviors is { } validTransitionBehaviors)
        {
            TransitionBehaviors = validTransitionBehaviors;
            if (!isExceptionalParameter)
            {
                ValidateTypePresence();
            }
        }
        else
        {
            TransitionBehaviors = FindDefaultTransitionBehaviors(javaType, jniType, nativeType, transitionDirection);
        }

        if (!isExceptionalParameter)
        {
            ValidateLinkComplete();
        }
    }

    public JavaType? JavaJniType { get; }

    public string IntermediateName { get; }

    public TransitionBehaviorSet TransitionBehaviors { get; }
    public TransitionDirection TransitionDirection { get; }

    public string Name { get; }
    public JavaType? JavaType { get; }
    public JniType? JniType { get; }
    public TypeDesc? NativeType { get; }
    public TransitionAction TransitionAction { get; }

    public GeneratedExpression TransitOrGenerateValue(TransitionKind transitionKind,
        MethodGenerationUnit generationUnit)
    {
        var behavior = TransitionBehaviors.GetBehavior(transitionKind);
        return behavior switch {
            TransitionBehavior.Transit => TransitionAction.TransitValue(Name, transitionKind, generationUnit),
            TransitionBehavior.Generate => TransitionAction.GenerateValue(transitionKind, generationUnit),
            _ => throw new InvalidOperationException($"Couldn't transit value {this}")
        };
    }

    public MethodParameter<JavaType>? AsJavaParameter() =>
        JavaType is not null ? new MethodParameter<JavaType>(JavaType, Name) : null;

    public MethodParameter<JavaType>? AsJavaJniParameter() =>
        JavaJniType is not null ? new MethodParameter<JavaType>(JavaJniType, Name) : null;

    public MethodParameter<JniType>? AsJniParameter() =>
        JniType is { } validJniType ? new MethodParameter<JniType>(validJniType, Name) : null;

    public MethodParameter<TypeDesc>? AsNativeParameter() =>
        NativeType is not null ? new MethodParameter<TypeDesc>(NativeType, Name) : null;

    private void ValidateTypePresence()
    {
        var javaSourcingBehavior = TransitionBehaviors.GetJavaSourcingBehavior(TransitionDirection);
        if (javaSourcingBehavior == TransitionBehavior.Transit)
        {
            if (JavaType is null || JniType is null)
            {
                throw new InvalidOperationException("Can't transit with no JavaType or JniType.");
            }
        }

        var nativeSourcingBehavior = TransitionBehaviors.GetNativeSourcingBehavior(TransitionDirection);
        if (nativeSourcingBehavior == TransitionBehavior.Transit)
        {
            if (JniType is null || NativeType is null)
            {
                throw new InvalidOperationException("Can't transit with no JniType or NativeType.");
            }
        }
    }

    private void ValidateLinkComplete()
    {
        var endTransition = TransitionBehaviors.GetFinalTransition(TransitionDirection);
        if (endTransition == TransitionBehavior.None)
        {
            throw new InvalidOperationException(
                $"The non-exceptional parameter {Name} doesn't have a valid ending transition.");
        }
    }

    private static TransitionBehaviorSet FindDefaultTransitionBehaviors(
        JavaType? javaType,
        JniType? jniType,
        TypeDesc? nativeType,
        TransitionDirection direction)
    {
        bool javaLayer = false, nativeLayer = false;

        if (javaType is not null && jniType is not null)
        {
            javaLayer = true;
        }

        if (jniType is not null && nativeType is not null)
        {
            nativeLayer = true;
        }

        if (direction == TransitionDirection.Downstream)
        {
            return new TransitionBehaviorSet {
                JavaToJni = javaLayer ? TransitionBehavior.Transit : TransitionBehavior.None,
                JniToNative = nativeLayer ? TransitionBehavior.Transit : TransitionBehavior.None
            };
        }
        else
        {
            return new TransitionBehaviorSet {
                NativeToJni = nativeLayer ? TransitionBehavior.Transit : TransitionBehavior.None,
                JniToJava = javaLayer ? TransitionBehavior.Transit : TransitionBehavior.None
            };
        }
    }
}
