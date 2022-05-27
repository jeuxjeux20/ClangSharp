// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using ClangSharp.Abstractions;
using ClangSharp.JNI.JNIGlue;

namespace ClangSharp.JNI.Generation.Method;

#nullable enable
internal sealed record CallNativeMethodOperation(NativeMethod Method)
    : NativeOperation(Method.ReturnType, TransformToOperationParameters(Method.Parameters))
{
    public override string GenerateRunExpression(MethodGenerationUnit generationUnit)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendMethodCallExpression(Method.Name, generationUnit.ParameterLinkages,
            link => link.TransitingParameter.GetNativeTransitExpression());

        return stringBuilder.ToString();
    }

    private static ImmutableArray<NativeOperationParameter> TransformToOperationParameters(
        IReadOnlyList<MethodParameter<TypeDesc>> methodParameters)
    {
        var builder = ImmutableArray.CreateBuilder<NativeOperationParameter>(methodParameters.Count);
        foreach (var parameter in methodParameters)
        {
            builder.Add(new NativeOperationParameter(parameter));
        }
        return builder.MoveToImmutable();
    }
}
