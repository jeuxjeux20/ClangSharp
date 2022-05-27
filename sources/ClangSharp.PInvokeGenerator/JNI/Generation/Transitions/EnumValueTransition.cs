// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using ClangSharp.Abstractions;
using ClangSharp.JNI.Generation.Method;

namespace ClangSharp.JNI.Generation.Transitions;

internal class EnumValueTransition : ValueTransition
{
    public EnumTypeDesc EnumType { get; }

    public EnumValueTransition(EnumTypeDesc enumType)
    {
        EnumType = enumType;
    }

    public override GeneratedExpression TransitValue(string valueExpression,
        TransitionKind transitionKind, MethodGenerationUnit generationUnit)
    {
        return transitionKind switch {
            TransitionKind.JavaToJni => valueExpression,
            TransitionKind.JniToNative => $"static_cast<{EnumType.Name}>({valueExpression})",

            TransitionKind.NativeToJni => $"static_cast<long>({valueExpression})",
            TransitionKind.JniToJava => valueExpression,
            _ => throw new UnsupportedJniScenarioException()
        };
    }
}
