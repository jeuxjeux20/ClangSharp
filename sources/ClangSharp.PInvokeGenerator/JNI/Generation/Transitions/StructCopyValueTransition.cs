// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using ClangSharp.Abstractions;
using ClangSharp.JNI.Generation.Method;

namespace ClangSharp.JNI.Generation.Transitions;

internal class StructCopyValueTransition : TransitionAction
{
    public string JavaStructClass { get; }
    public RecordTypeDesc NativeType { get; }

    public StructCopyValueTransition(string javaStructClass, RecordTypeDesc nativeType)
    {
        JavaStructClass = javaStructClass;
        NativeType = nativeType;
    }

    public override GeneratedExpression TransitValue(string valueExpression,
        TransitionKind transitionKind, MethodGenerationUnit generationUnit)
    {
        return transitionKind switch {
            TransitionKind.JavaToJni => $"{valueExpression}.getHandle()",
            TransitionKind.JniToNative => $"*FumoCement::toNativePointer<{NativeType.Name}>({valueExpression})",

            TransitionKind.NativeToJni => $"FumoCement::toJavaPointer(new {NativeType.Name}({valueExpression}))",
            TransitionKind.JniToJava => $"{JavaStructClass}.getTrackedAndOwned({valueExpression})",
            _ => throw new UnsupportedJniScenarioException()
        };
    }
}
