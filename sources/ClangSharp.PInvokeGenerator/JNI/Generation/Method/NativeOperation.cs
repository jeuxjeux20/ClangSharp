// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Collections.Immutable;
using ClangSharp.Abstractions;

namespace ClangSharp.JNI.Generation.Method;

#nullable enable
internal abstract record NativeOperation(TypeDesc ReturnType, ImmutableArray<NativeOperationParameter> Parameters)
{
    public virtual void PreparePreIntermediateExpression(IIndentedWriter writer, MethodGenerationUnit generationUnit)
    {
    }

    public virtual void PreparePostIntermediateExpression(IIndentedWriter writer, MethodGenerationUnit generationUnit)
    {
    }

    public abstract string GenerateRunExpression(MethodGenerationUnit generationUnit);
}

internal record NativeOperationParameter(TypeDesc Type, string Name)
{
    public NativeOperationParameter(MethodParameter<TypeDesc> parameter) : this(parameter.Type, parameter.Name)
    {
    }
}
