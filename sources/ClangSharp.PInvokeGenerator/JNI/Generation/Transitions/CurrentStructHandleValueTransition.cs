// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

#nullable enable
using ClangSharp.Abstractions;
using ClangSharp.JNI.Generation.Method;

namespace ClangSharp.JNI.Generation.Transitions;

internal sealed class CurrentStructHandleValueTransition : TransitionAction
{
    public RecordTypeDesc Struct { get; }

    public CurrentStructHandleValueTransition(RecordTypeDesc @struct)
    {
        Struct = @struct;
    }

    public override GeneratedExpression TransitValue(string valueExpression,
        TransitionKind transitionKind, MethodGenerationUnit generationUnit)
    {
        return transitionKind switch {
            TransitionKind.JniToNative => $"FumoCement::toNativePointer<{Struct.Name}>({valueExpression})",
            _ => throw new UnsupportedJniScenarioException()
        };
    }

    public override GeneratedExpression GenerateValue(TransitionKind transitionKind, MethodGenerationUnit generationUnit)
    {
        return transitionKind switch {
            TransitionKind.JavaToJni => "getHandle()",
            _ => throw new UnsupportedJniScenarioException()
        };
    }
}
