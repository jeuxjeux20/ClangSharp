// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Collections.Immutable;
using ClangSharp.Abstractions;
using ClangSharp.JNI.Java;

namespace ClangSharp.JNI.Generation.Struct;

internal record StructTarget(string NativeName, ImmutableArray<StructField> Fields) : TransformationTarget
{
    public RecordTypeDesc NativeStructType { get; } = new(NativeName);
}
