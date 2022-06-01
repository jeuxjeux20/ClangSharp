// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Immutable;
using System.Text;
using ClangSharp.JNI.Generation;
using ClangSharp.JNI.Generation.Method;
using ClangSharp.JNI.Generation.Transitions;

namespace ClangSharp.JNI.Java;

internal static class JavaTransitionWriter
{
    public static void Write(IIndentedWriter writer, MethodGenerationUnit methodGen,
        TransitionKind parameterTransition, Func<ImmutableArray<TransitingMethodParameter>, string> makeFinalMethodCall)
    {
        string GetParameterType(TransitingMethodParameter parameter)
        {
            return parameterTransition == TransitionKind.JavaToJni
                ? parameter.JavaJniType!.ToString()
                : parameter.JavaType!.ToString();
        }

        var returnValueTransition = parameterTransition == TransitionKind.JavaToJni
            ? TransitionKind.JniToJava
            : TransitionKind.JavaToJni;

        var parameters = methodGen.GetSortedTransitingParameters(parameterTransition);
        foreach (var parameter in parameters)
        {
            writer.WriteIndentedLine($"{GetParameterType(parameter)} {parameter.IntermediateName} = ");
            writer.Write(parameter.TransitOrGenerateValue(parameterTransition, methodGen));
            writer.Write(';');
        }

        // Return values

        var returnLinkage = methodGen.ReturnValueLinkage;

        writer.WriteIndentedLine();
        if (returnLinkage is not null)
        {
            string finalExpression;
            if (returnLinkage.TransitionAction.NeedsIntermediateReturnValue)
            {
                var returnType = parameterTransition == TransitionKind.JavaToJni
                    ? returnLinkage.JavaJniType
                    : returnLinkage.JavaType;

                writer.Write($"{returnType} {JniGenerationNamings.Internal.ReturnValueIntermediate} = ");
                writer.Write(makeFinalMethodCall(parameters));
                writer.Write(";");
                writer.WriteIndentedLine();

                finalExpression = JniGenerationNamings.Internal.ReturnValueIntermediate;
            }
            else
            {
                finalExpression = makeFinalMethodCall(parameters);
            }

            writer.Write("return ");
            writer.Write(returnLinkage.TransitValue(finalExpression, returnValueTransition, methodGen));
            writer.Write(";");
        }
        else
        {
            writer.Write(makeFinalMethodCall(parameters));
            writer.Write(";");
        }
    }
}
