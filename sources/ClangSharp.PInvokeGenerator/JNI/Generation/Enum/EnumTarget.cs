// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Collections.Immutable;

namespace ClangSharp.JNI.Generation.Enum;

internal record EnumTarget(string Name, ImmutableArray<EnumConstant> Constants) : TransformationTarget;
