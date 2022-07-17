// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Collections.Immutable;

namespace ClangSharp.JNI.Java;

internal sealed class BodylessJavaMethod : Method<JavaType>
{
    public BodylessJavaMethod(string name,
        JavaType returnType,
        ImmutableArray<MethodParameter<JavaType>> parameters,
        string visibility,
        bool isNative = true,
        bool isStatic = true) : base(name, returnType, parameters)
    {
        Visibility = visibility;
        IsNative = isNative;
        IsStatic = isStatic;
    }

    public string Visibility { get; }
    public bool IsNative { get; }
    public bool IsStatic { get; }
}
