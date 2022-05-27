// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using ClangSharp.Abstractions;
using ClangSharp.JNI.Java;

namespace ClangSharp.JNI.Generation;

internal static class WellKnownJniTypes
{
    public static class Native
    {
        public static readonly PointerTypeDesc FunctionPointerContextPtr
            = new(new RecordTypeDesc("FumoCement::FuctionPointerContext"));
    }

    public static class Java
    {
        public static ObjectJavaType FunctionPointer(string callbackType)
            => new("com.github.novelrt.fumocement", "FunctionPointer", new[] { callbackType });
    }
}
