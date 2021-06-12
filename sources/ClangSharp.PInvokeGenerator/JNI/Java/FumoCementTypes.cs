// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace ClangSharp.JNI.Java
{
    internal static class FumoCementTypes
    {
        public const string Package = "com.github.novelrt.fumocement";

        public static readonly ObjectJavaType StringDeletionBehaviour
            = new(Package, "StringDeletionBehaviour");

        public static ObjectJavaType FunctionPointer(string callback)
            => new(Package, "FunctionPointer", new[] { callback });
    }
}
