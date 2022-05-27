// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Collections.Immutable;

namespace ClangSharp.JNI.JNIGlue;

internal sealed class JniGlueMethod : Method<JniType>
{
    public JniGlueMethod(string name,
        JniType returnType,
        ImmutableArray<MethodParameter<JniType>> parameters,
        string containingType) : base(name, returnType, parameters)
    {
        ContainingType = containingType;
    }

    public string ContainingType { get; }
}
