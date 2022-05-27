// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using ClangSharp.JNI.Generation.Method;

namespace ClangSharp.JNI.Generation.Transitions;

internal class CastFunctionPointerContextTransition : ValueTransition
{
    public override GeneratedExpression TransitValue(string valueExpression,
        TransitionKind transitionKind, MethodGenerationUnit generationUnit)
    {
        return transitionKind switch {
            TransitionKind.NativeToJni => $"static_cast<FumoCement::FunctionPointerContext*>({valueExpression})",
            _ => throw new UnsupportedJniScenarioException()
        };
    }
}
