// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

#nullable enable
using ClangSharp.Abstractions;
using ClangSharp.JNI.Generation.Method;

namespace ClangSharp.JNI.Generation.Transitions;

internal sealed class PointerValueTransition : TransitionAction
{
    public PointerTypeDesc PointerType { get; }

    public PointerValueTransition(PointerTypeDesc pointerType)
    {
        PointerType = pointerType;
    }

    public override GeneratedExpression TransitValue(string valueExpression,
        TransitionKind transitionKind, MethodGenerationUnit generationUnit)
    {
        return transitionKind switch {
            TransitionKind.JavaToJni => valueExpression,
            TransitionKind.JniToNative
                => $"FumoCement::toNativePointer<{PointerType.PointeeType.AsRawString}>({valueExpression})",

            TransitionKind.NativeToJni => $"FumoCement::toJavaPointer({valueExpression})",
            TransitionKind.JniToJava => valueExpression,

            _ => throw new UnsupportedJniScenarioException()
        };
    }
}
