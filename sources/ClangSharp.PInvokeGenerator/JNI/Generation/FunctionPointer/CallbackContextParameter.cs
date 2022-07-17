// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using ClangSharp.Abstractions;
using ClangSharp.JNI.Generation.Method;

namespace ClangSharp.JNI.Generation.FunctionPointer;

internal record CallbackContextParameter() : NativeOperationParameter(
    new PointerTypeDesc(BuiltinTypeDesc.Void),
    JniInternalNames.CallbackLambdaContext);
