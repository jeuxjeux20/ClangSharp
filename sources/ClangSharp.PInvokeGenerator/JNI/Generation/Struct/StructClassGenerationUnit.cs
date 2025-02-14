﻿// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using ClangSharp.JNI.Generation.Configuration;
using ClangSharp.JNI.Java;

namespace ClangSharp.JNI.Generation.Struct;

internal class StructClassGenerationUnit : GenerationUnit
{
    public string JavaName { get; }
    public string NativeName { get; }

    public ObjectJavaType JavaStructType { get; }

    public StructClassGenerationUnit(StructTarget target, JniGenerationContext context, StructRule rule)
    {
        JavaName = rule.NameOverride ?? JavaConventions.EscapeName(target.NativeName);
        NativeName = target.NativeName;
        JavaStructType = context.NestedTypeInContainer(JavaName);
    }

    public const string AllocateStructMethodName = "allocateStruct";
    public const string DestroyStructMethodName = "destroyStruct";

    public const string OverwriteMethodName = "overwrite";
}
