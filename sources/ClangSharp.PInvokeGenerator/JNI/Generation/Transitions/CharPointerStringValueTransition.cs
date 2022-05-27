// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using ClangSharp.JNI.Generation.Method;

namespace ClangSharp.JNI.Generation.Transitions;

#nullable enable
internal class CharPointerStringValueTransition : TransitionAction
{
    public string? StringDeletionParameterName { get; }

    public CharPointerStringValueTransition(string? stringDeletionParameterName)
    {
        StringDeletionParameterName = stringDeletionParameterName;
    }

    public override bool NeedsIntermediateReturnValue => true;
    public override bool NeedsCppToCTransformation => true;

    public override GeneratedExpression TransitValue(string valueExpression,
        TransitionKind transitionKind, MethodGenerationUnit generationUnit)
    {
        return (StringDeletionParameterName, direction: transitionKind) switch
        {
            (_, TransitionKind.JavaToJni)
                => $"{valueExpression}.getBytes()",
            (null, TransitionKind.JniToNative)
                => $"FumoCement::toCppString({JavaConventions.JniEnvVariable}, {valueExpression})",

            ({} deletion, TransitionKind.NativeToJni) =>
                $"FumoCement::toJavaStringBytes({JavaConventions.JniEnvVariable}, {valueExpression}, {deletion})",
            (null, TransitionKind.NativeToJni) =>
                $"FumoCement::toJavaStringBytes({JavaConventions.JniEnvVariable}, {valueExpression}, false)",
            (_, TransitionKind.JniToJava)
                => $"{valueExpression} == null ? null : new String({valueExpression})",

            _ => throw new UnsupportedJniScenarioException()
        };
    }
}
