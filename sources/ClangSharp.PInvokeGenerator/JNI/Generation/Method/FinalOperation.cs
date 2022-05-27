// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Collections.Immutable;
using ClangSharp.Abstractions;

namespace ClangSharp.JNI.Generation.Method;

#nullable enable
internal abstract record FinalOperation(TypeDesc ReturnType, ImmutableArray<FinalOperationParameter> Parameters)
{
    public abstract string GenerateRunExpression(MethodGenerationUnit generationUnit);
}

internal record FinalOperationParameter(TypeDesc Type, string Name)
{
    public FinalOperationParameter(MethodParameter<TypeDesc> parameter) : this(parameter.Type, parameter.Name)
    {
    }
}
