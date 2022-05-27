// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;

namespace ClangSharp.JNI.Generation.Transitions;

internal readonly record struct TransitionBehaviorSet
{
    public TransitionBehavior JavaToJni { get; init; }
    public TransitionBehavior JniToJava { get; init; }
    public TransitionBehavior JniToNative { get; init; }
    public TransitionBehavior NativeToJni { get; init; }

    public TransitionBehavior GetBehavior(TransitionKind kind)
    {
        return kind switch {
            TransitionKind.JavaToJni => JavaToJni,
            TransitionKind.JniToNative => JniToNative,
            TransitionKind.NativeToJni => NativeToJni,
            TransitionKind.JniToJava => JniToJava,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    public TransitionBehavior GetNativeSourcingBehavior(TransitionDirection direction)
    {
        return direction switch {
            TransitionDirection.Upstream => NativeToJni,
            TransitionDirection.Downstream => JniToNative,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }

    public TransitionBehavior GetJavaSourcingBehavior(TransitionDirection direction)
    {
        return direction switch {
            TransitionDirection.Upstream => JniToJava,
            TransitionDirection.Downstream => JavaToJni,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }

    public TransitionBehavior GetFinalTransition(TransitionDirection direction)
    {
        return direction switch {
            TransitionDirection.Upstream => JniToJava,
            TransitionDirection.Downstream => JniToNative,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }

    public bool Supports(TransitionKind kind)
    {
        return GetBehavior(kind) != TransitionBehavior.None;
    }
}
