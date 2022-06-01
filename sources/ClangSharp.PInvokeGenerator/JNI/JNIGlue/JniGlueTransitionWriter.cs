// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using ClangSharp.JNI.Generation;
using ClangSharp.JNI.Generation.Method;
using ClangSharp.JNI.Generation.Transitions;

namespace ClangSharp.JNI.JNIGlue;

internal class JniGlueTransitionWriter
{
    public static void Write(IIndentedWriter writer, MethodGenerationUnit methodGen, TransitionKind parameterTransition)
    {
        var returnValueTransition = parameterTransition == TransitionKind.JniToNative
            ? TransitionKind.NativeToJni
            : TransitionKind.JniToNative;

        methodGen.NativeOperation.PreparePreIntermediateExpression(writer, methodGen);

        foreach (var parameter in methodGen.GetSortedTransitingParameters(parameterTransition))
        {
            var intermediateName = parameter.IntermediateName;

            writer.WriteIndentedLine($"auto&& {intermediateName} = ");
            writer.Write(parameter.TransitOrGenerateValue(parameterTransition, methodGen));
            writer.Write(';');
        }

        methodGen.NativeOperation.PreparePostIntermediateExpression(writer, methodGen);

        // Call the method (and put the return value in a variable if any)
        writer.WriteIndentedLine();

        var nativeOpExpression = methodGen.NativeOperation.GenerateRunExpression(methodGen);
        var returnLinkage = methodGen.ReturnValueLinkage;
        if (returnLinkage is not null)
        {
            string finalExpression;
            if (returnLinkage.TransitionAction.NeedsIntermediateReturnValue)
            {
                writer.Write($"auto&& {JniGenerationNamings.Internal.ReturnValueIntermediate} = ");
                writer.Write(nativeOpExpression);
                writer.Write(";");
                writer.WriteIndentedLine();

                finalExpression = JniGenerationNamings.Internal.ReturnValueIntermediate;
            }
            else
            {
                finalExpression = nativeOpExpression;
            }
            writer.Write("return ");
            writer.Write(returnLinkage.TransitValue(finalExpression, returnValueTransition, methodGen));
            writer.Write(";");
        }
        else
        {
            writer.Write(nativeOpExpression);
            writer.Write(";");
        }
    }
}
