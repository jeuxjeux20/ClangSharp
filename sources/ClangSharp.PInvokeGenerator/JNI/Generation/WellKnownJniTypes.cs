// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using ClangSharp.Abstractions;
using ClangSharp.JNI.Java;

namespace ClangSharp.JNI.Generation;

internal static class WellKnownJniTypes
{
    public static class Java
    {
        public const string FumoCementPackage = "com.github.novelrt.fumocement";

        public static ObjectJavaType FunctionPointer(string callbackType)
            => new(FumoCementPackage, "FunctionPointer", new[] { callbackType });

        public static readonly ObjectJavaType StringDeletionBehaviour
            = new(FumoCementPackage, "StringDeletionBehaviour");
    }
}
