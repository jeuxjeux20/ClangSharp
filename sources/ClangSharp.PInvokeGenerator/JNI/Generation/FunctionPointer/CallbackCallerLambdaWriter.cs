// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using ClangSharp.JNI.Generation.Method;
using ClangSharp.JNI.Generation.Transitions;

namespace ClangSharp.JNI.Generation.FunctionPointer;

internal static class CallbackCallerLambdaWriter
{
    public static void Write(IIndentedWriter writer, UpstreamMethodGenerationUnit upstreamMethodGen)
    {
        var lambda = upstreamMethodGen.JniCallbackCallerLambda;

        writer.Write("[]");
        writer.RawBuilder.AppendMethodParameters(lambda.Parameters, p => $"{p.Type.AsRawString} {p.Name}");
        writer.Write(" -> ");
        writer.Write(lambda.ReturnType);
        writer.WriteBlockStart();

        foreach (var parameter in upstreamMethodGen.GetSortedTransitingParameters(TransitionKind.NativeToJni))
        {
            writer.WriteIndentedLine($"auto&& {parameter.IntermediateName} = ");
            writer.Write(parameter.TransitOrGenerateValue(TransitionKind.NativeToJni, upstreamMethodGen));
            writer.Write(';');
        }

        // Call the method (and put the return value in a variable if any)
        writer.WriteIndentedLine();

        var returnValueLinkage = upstreamMethodGen.ReturnValueLinkage;
        if (returnValueLinkage is not null)
        {
            writer.Write("return ");
        }

        var expression = upstreamMethodGen.NativeOperation.GenerateRunExpression(upstreamMethodGen);
        if (returnValueLinkage is not null)
        {
            writer.Write(returnValueLinkage.TransitValue(expression, TransitionKind.JniToNative, upstreamMethodGen));
        }
        else
        {
            writer.Write(expression);
        }

        writer.Write(";");

        writer.WriteBlockEnd();
    }
}
