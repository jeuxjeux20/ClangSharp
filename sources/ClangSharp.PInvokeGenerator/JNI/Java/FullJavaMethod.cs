// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Immutable;
using System.Text;

namespace ClangSharp.JNI.Java;

internal sealed class FullJavaMethod : Method<JavaType>
{
    private readonly Lazy<string> _jniSignatureLazy;

    public FullJavaMethod(string name,
        JavaType returnType,
        ImmutableArray<MethodParameter<JavaType>> parameters,
        bool isStatic) : base(name, returnType, parameters)
    {
        IsStatic = isStatic;
        _jniSignatureLazy = new Lazy<string>(MakeJniTypeSignature);
    }

    private string MakeJniTypeSignature()
    {
        var builder = new StringBuilder();
        builder.Append('(');
        foreach (var parameter in Parameters)
        {
            builder.Append(parameter.Type.JniTypeSignature);
        }

        builder.Append(')');

        builder.Append(ReturnType.JniTypeSignature);
        return builder.ToString();
    }

    public bool IsStatic { get; }

    public string JniSignature => _jniSignatureLazy.Value;
}
