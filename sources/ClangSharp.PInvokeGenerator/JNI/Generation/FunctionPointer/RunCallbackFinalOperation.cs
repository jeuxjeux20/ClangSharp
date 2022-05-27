// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using ClangSharp.Abstractions;
using ClangSharp.JNI.Generation.Method;
using ClangSharp.JNI.Java;

namespace ClangSharp.JNI.Generation.FunctionPointer;

internal record RunCallbackFinalOperation(FunctionProtoTypeDesc FunctionPointerType, ObjectJavaType JavaCallbackType)
    : FinalOperation(FunctionPointerType.ReturnType, MakeParameters(FunctionPointerType, JavaCallbackType))
{
    public override string GenerateRunExpression(MethodGenerationUnit generationUnit)
    {
        var returnLinkage = generationUnit.ReturnValueLinkage;
        var parameterLinkages = generationUnit.ParameterLinkages;

        var builder = new StringBuilder();

        // TODO: probably don't use JavaType?
        var contextParameter = parameterLinkages[^1].TransitingParameter.IntermediateName;
        var method = returnLinkage?.JavaType switch {
            null => "CallStaticVoidMethod",
            { Kind: JavaTypeKind.Object or JavaTypeKind.Array } => "CallStaticObjectMethod",
            { Kind: JavaTypeKind.Boolean } => "CallStaticBoolMethod",
            { Kind: JavaTypeKind.Byte } => "CallStaticByteMethod",
            { Kind: JavaTypeKind.Char } => "CallStaticCharMethod",
            { Kind: JavaTypeKind.Short } => "CallStaticShortMethod",
            { Kind: JavaTypeKind.Int } => "CallStaticIntMethod",
            { Kind: JavaTypeKind.Long } => "CallStaticLongMethod",
            { Kind: JavaTypeKind.Float } => "CallStaticFloatMethod",
            { Kind: JavaTypeKind.Double } => "CallStaticDoubleMethod",
            { } other => throw new UnsupportedJniScenarioException($"Cannot use return type: {other}")
        };

        // (context->getEnv())->CallStaticXMethod(arg1, arg2, arg3);
        builder.Append('(');
        builder.Append(contextParameter);
        builder.Append("->getEnv())->");
        builder.AppendMethodCallExpression(method, parameterLinkages.SkipLast(1).ToArray());

        return builder.ToString();
    }

    private static ImmutableArray<FinalOperationParameter> MakeParameters(FunctionProtoTypeDesc functionPointerType,
        ObjectJavaType javaCallbackType)
    {
        var builder = ImmutableArray.CreateBuilder<FinalOperationParameter>(functionPointerType.Parameters.Count + 3);

        builder.Add(new CallbackClassIdParameter());
        builder.Add(new CallbackMethodIdParameter());
        builder.Add(new CallbackObjectParameter(javaCallbackType));

        for (var i = 0; i < functionPointerType.Parameters.Count - 1; i++)
        {
            var parameter = functionPointerType.Parameters[i];
            builder.Add(new FinalOperationParameter(parameter, $"proxyParam{i}"));
        }

        builder.Add(new CallbackContextParameter());

        return builder.MoveToImmutable();
    }
}
