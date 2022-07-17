// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using ClangSharp.Abstractions;
using ClangSharp.JNI.Generation.Method;

namespace ClangSharp.JNI.Generation.Transitions;
#nullable enable
internal class StructCopyValueTransition : TransitionAction
{
    public string? JavaStructClass { get; }
    public RecordTypeDesc NativeType { get; }

    private bool HasJavaStruct => JavaStructClass is not null;

    public StructCopyValueTransition(string? javaStructClass, RecordTypeDesc nativeType)
    {
        JavaStructClass = javaStructClass;
        NativeType = nativeType;
    }

    public override GeneratedExpression TransitValue(string valueExpression,
        TransitionKind transitionKind, MethodGenerationUnit generationUnit)
    {
        // We may have no struct found for some reason (such as an exclusion in configs)
        // Instead, we just directly use a pointer to the struct.
        return transitionKind switch {
            TransitionKind.JavaToJni => HasJavaStruct ? $"{valueExpression}.getHandle()" : valueExpression,
            TransitionKind.JniToNative => $"*FumoCement::toNativePointer<{NativeType.Name}>({valueExpression})",

            TransitionKind.NativeToJni => $"FumoCement::toJavaPointer(new {NativeType.Name}({valueExpression}))",
            TransitionKind.JniToJava => HasJavaStruct ?
                $"{JavaStructClass}.getTrackedAndOwned({valueExpression})" : valueExpression,
            _ => throw new UnsupportedJniScenarioException()
        };
    }
}
