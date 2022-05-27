// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace ClangSharp.JNI;

internal readonly struct MethodParameter<T>
{
    public MethodParameter(T type, string name)
    {
        Type = type;
        Name = name;
    }

    public T Type { get; }
    public string Name { get; }

    public override string ToString() => $"{Type} {Name}";
}
