// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using ClangSharp.JNI.Generation.FunctionPointer;
using ClangSharp.JNI.Generation.Method;

namespace ClangSharp.JNI.Generation.Transitions;

internal class CallbackCallerLambdaTransition : TransitionAction
{
    private readonly FunctionPointerTransformationUnit _functionPointerUnit;

    public CallbackCallerLambdaTransition(FunctionPointerTransformationUnit functionPointerUnit)
    {
        _functionPointerUnit = functionPointerUnit;
    }

    public override GeneratedExpression TransitValue(string valueExpression, TransitionKind transitionKind,
        MethodGenerationUnit generationUnit)
    {
        throw new UnsupportedJniScenarioException();
    }

    public override GeneratedExpression GenerateValue(TransitionKind transitionKind,
        MethodGenerationUnit generationUnit)
    {
        if (transitionKind != TransitionKind.JniToNative)
        {
            throw new UnsupportedJniScenarioException();
        }

        var upstreamMethodGen = _functionPointerUnit.MethodGenerationUnit;
        return new GeneratedExpression(writer => CallbackCallerLambdaWriter.Write(writer, upstreamMethodGen));
    }
}
