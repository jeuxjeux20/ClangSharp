// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using ClangSharp.Abstractions;

namespace ClangSharp.JNI.Generation.Method;

#nullable enable
internal sealed record CallNativeMethodFinalOperation(NativeMethod Method)
    : FinalOperation(Method.ReturnType, TransformToOperationParameters(Method.Parameters))
{
    public override string GenerateRunExpression(MethodGenerationUnit generationUnit)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendMethodCallExpression(Method.Name, generationUnit.ParameterLinkages);

        return stringBuilder.ToString();
    }

    private static ImmutableArray<FinalOperationParameter> TransformToOperationParameters(
        IReadOnlyList<MethodParameter<TypeDesc>> methodParameters)
    {
        var builder = ImmutableArray.CreateBuilder<FinalOperationParameter>(methodParameters.Count);
        foreach (var parameter in methodParameters)
        {
            builder.Add(new FinalOperationParameter(parameter));
        }
        return builder.MoveToImmutable();
    }
}
