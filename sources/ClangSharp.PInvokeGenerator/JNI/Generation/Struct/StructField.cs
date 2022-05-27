// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using ClangSharp.Abstractions;

namespace ClangSharp.JNI.Generation.Struct;

internal readonly record struct StructField(TypeDesc Type, string Name);
