// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

#nullable enable
using System;

namespace ClangSharp.JNI.Generation.Transitions;

[Flags]
internal enum TransitionKind
{
    None = 0,
    JavaToJni = 1 << 0,
    JniToNative = 1 << 1,

    NativeToJni = 1 << 2,
    JniToJava = 1 << 3,
}
