// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Collections.Immutable;

namespace ClangSharp.JNI.Java;

internal sealed class BodylessJavaMethod : Method<JavaType>
{
    public BodylessJavaMethod(string name,
        JavaType returnType,
        ImmutableArray<MethodParameter<JavaType>> parameters,
        bool isNative = true,
        bool isStatic = true) : base(name, returnType, parameters)
    {
        IsNative = isNative;
        IsStatic = isStatic;
    }

    public bool IsNative { get; }
    public bool IsStatic { get; }
}
