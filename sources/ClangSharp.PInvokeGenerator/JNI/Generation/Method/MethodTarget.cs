// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace ClangSharp.JNI.Generation.Method;

#nullable enable
internal sealed record MethodTarget(NativeMethod Method) : TransformationTarget;
