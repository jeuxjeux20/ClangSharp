// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using ClangSharp.Abstractions;
using ClangSharp.JNI.Generation.Method;
using ClangSharp.JNI.Java;
using static ClangSharp.JNI.Generation.JniInternalNames;

namespace ClangSharp.JNI.Generation.FunctionPointer;
#nullable enable
internal record RunCallbackOperation(FunctionProtoTypeDesc FunctionPointerType, ObjectJavaType JavaCallbackType)
    : NativeOperation(FunctionPointerType.ReturnType, MakeParameters(FunctionPointerType, JavaCallbackType))
{
    public override void PreparePreIntermediateExpression(IIndentedWriter writer, MethodGenerationUnit generationUnit)
    {
        if (generationUnit is not UpstreamMethodGenerationUnit upstreamMethodGen)
        {
            throw new ArgumentException(null, nameof(generationUnit));
        }

        writer.WriteIndentedLine($"auto&& {CallbackLambdaContext} = " +
                                 $"static_cast<FumoCement::FunctionPointerContext*>({upstreamMethodGen.CallbackObjectParameter.Name});");
    }

    public override void PreparePostIntermediateExpression(IIndentedWriter writer, MethodGenerationUnit generationUnit)
    {
        if (generationUnit is not UpstreamMethodGenerationUnit upstreamMethodGen)
        {
            throw new ArgumentException(null, nameof(generationUnit));
        }

        writer.WriteIndentedLine($"auto&& {CallbackLambdaClassId} = FumoCement::getCachedClass<");
        writer.RawBuilder.AppendTemplateString(upstreamMethodGen.CallbackType.FullJniClass);
        writer.Write($">({CallbackLambdaContext}->getEnv());");

        writer.WriteIndentedLine($"auto&& {CallbackLambdaMethodId} = FumoCement::getCachedStaticMethod<");
        writer.RawBuilder.AppendTemplateString(upstreamMethodGen.CallbackType.FullJniClass);
        writer.Write(", ");
        writer.RawBuilder.AppendTemplateString(upstreamMethodGen.CallbackCallerMethod.Name);
        writer.Write(", ");
        writer.RawBuilder.AppendTemplateString(upstreamMethodGen.CallbackCallerMethod.JniSignature);
        writer.Write($">({CallbackLambdaContext}->getEnv());");
    }

    public override string GenerateRunExpression(MethodGenerationUnit generationUnit)
    {
        var returnLinkage = generationUnit.ReturnValueLinkage;
        var parameterLinkages = generationUnit.ParameterLinkages;

        var builder = new StringBuilder();

        var method = returnLinkage?.JniType switch {
            null or { Kind: JavaTypeKind.Void } => "CallStaticVoidMethod",
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
        builder
            .Append($"({CallbackLambdaContext}->getEnv())->{method}({CallbackLambdaClassId}, {CallbackLambdaMethodId}, ")
            .AppendMethodParameters(parameterLinkages,
            linkage => linkage.TransitingParameter.GetNativeTransitExpression(),
            removeOpeningParenthesis: true);

        return builder.ToString();
    }

    private static ImmutableArray<NativeOperationParameter> MakeParameters(FunctionProtoTypeDesc functionPointerType,
        ObjectJavaType javaCallbackType)
    {
        var builder = ImmutableArray.CreateBuilder<NativeOperationParameter>(functionPointerType.Parameters.Count);

        for (var i = 0; i < functionPointerType.Parameters.Count - 1; i++)
        {
            var parameter = functionPointerType.Parameters[i];
            builder.Add(new NativeOperationParameter(parameter, $"proxyParam{i}"));
        }
        builder.Add(new CallbackObjectParameter(javaCallbackType));

        return builder.MoveToImmutable();
    }
}
