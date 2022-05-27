// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using ClangSharp.JNI.Generation.Method;

namespace ClangSharp.JNI.Generation.FunctionPointer;

internal record CallbackMethodIdParameter() : NativeOperationParameter(JniType.JMethodId.AsNative(), "func$methodId");
