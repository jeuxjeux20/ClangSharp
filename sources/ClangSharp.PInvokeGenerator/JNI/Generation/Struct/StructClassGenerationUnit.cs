// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using ClangSharp.JNI.Java;

namespace ClangSharp.JNI.Generation.Struct;

internal class StructClassGenerationUnit : GenerationUnit
{
    public string JavaName { get; }
    public string NativeName { get; }

    public ObjectJavaType JavaStructType { get; }

    public StructClassGenerationUnit(StructTarget target)
    {
        JavaName = target.JavaName;
        NativeName = target.NativeName;
        JavaStructType = target.JavaStructType;
    }

    public const string AllocateStructMethodName = "allocateStruct";
    public const string DestroyStructMethodName = "destroyStruct";

    public const string OverwriteMethodName = "overwrite";
}
