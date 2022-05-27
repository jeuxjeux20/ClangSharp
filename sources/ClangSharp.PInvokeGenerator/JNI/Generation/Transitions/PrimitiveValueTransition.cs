// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

#nullable enable
using ClangSharp.JNI.Generation.Method;

namespace ClangSharp.JNI.Generation.Transitions;

internal sealed class PrimitiveValueTransition : TransitionAction
{
    public override GeneratedExpression TransitValue(string valueExpression,
        TransitionKind transitionKind, MethodGenerationUnit generationUnit)
    {
        return transitionKind switch {
            TransitionKind.JavaToJni or TransitionKind.JniToJava => $"{valueExpression}",
            TransitionKind.JniToNative => $"FumoCement::toNativePrimitive({valueExpression})",
            TransitionKind.NativeToJni => $"FumoCement::toJavaPrimitive({valueExpression})",
            _ => throw new UnsupportedJniScenarioException()
        };
    }
}
