﻿// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using ClangSharp.Abstractions;
using ClangSharp.JNI.Generation.Method;

namespace ClangSharp.JNI.Generation.Struct;

internal record StructHandleParameter(RecordTypeDesc StructType)
    : NativeOperationParameter(new PointerTypeDesc(StructType), "handle");
