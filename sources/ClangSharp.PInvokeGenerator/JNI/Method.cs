// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ClangSharp.JNI;

internal abstract class Method<T>
{
    public Method(string name, T returnType, ImmutableArray<MethodParameter<T>> parameters)
    {
        Name = name;
        ReturnType = returnType;
        Parameters = parameters.ToArray();
    }

    public string Name { get; }
    public T ReturnType { get; }
    public IReadOnlyList<MethodParameter<T>> Parameters { get; }
}
