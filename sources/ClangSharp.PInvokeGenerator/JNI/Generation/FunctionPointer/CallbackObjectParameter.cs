﻿// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using ClangSharp.JNI.Generation.Method;
using ClangSharp.JNI.Java;

namespace ClangSharp.JNI.Generation.FunctionPointer;

internal record CallbackObjectParameter(ObjectJavaType JavaCallbackType)
    : NativeOperationParameter(JniType.JObject.AsNative(), "func$callbackObject");
