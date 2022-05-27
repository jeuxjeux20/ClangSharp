// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Text;
using ClangSharp.JNI.Generation.Method;
using ClangSharp.JNI.Generation.Transitions;

namespace ClangSharp.JNI.Java;

internal static class JavaConstructs
{
    public static void WriteMethodTransition(IIndentedWriter writer, MethodGenerationUnit methodGen,
        TransitionKind parameterTransition, string finalMethod)
    {
        var returnValueTransition = parameterTransition == TransitionKind.JavaToJni
            ? TransitionKind.JniToJava
            : TransitionKind.JavaToJni;

        var parameters = methodGen.GetTransitingParameters(parameterTransition);
        foreach (var parameter in parameters)
        {
            writer.WriteIndentedLine($"{parameter.JavaJniType} {parameter.IntermediateName} = ");
            writer.Write(parameter.TransitOrGenerateValue(parameterTransition, methodGen));
            writer.Write(';');
        }

        var returnLinkage = methodGen.ReturnValueLinkage;

        writer.WriteNewLine();
        writer.WriteIndentation();
        if (returnLinkage is not null)
        {
            writer.Write("return ");
        }

        // Call the target method.
        var finalMethodCall = new StringBuilder()
            .AppendMethodCallExpression(finalMethod, parameters)
            .ToString();
        if (returnLinkage is not null)
        {
            writer.Write(returnLinkage.TransitValue(finalMethodCall, returnValueTransition, methodGen));
        }
        else
        {
            writer.Write(finalMethodCall);
        }

        writer.Write(";");
    }
}
