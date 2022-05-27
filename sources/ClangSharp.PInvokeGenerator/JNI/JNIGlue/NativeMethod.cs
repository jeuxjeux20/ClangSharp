// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Collections.Immutable;
using ClangSharp.Abstractions;

namespace ClangSharp.JNI.JNIGlue;

internal sealed class NativeMethod : Method<TypeDesc>
{
    public NativeMethod(string name,
        TypeDesc returnType,
        ImmutableArray<MethodParameter<TypeDesc>> parameters) : base(name, returnType, parameters)
    {
    }
}
