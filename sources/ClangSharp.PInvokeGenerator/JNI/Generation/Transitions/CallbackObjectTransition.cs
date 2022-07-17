// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Linq;
using ClangSharp.JNI.Generation.FunctionPointer;
using ClangSharp.JNI.Generation.Method;

namespace ClangSharp.JNI.Generation.Transitions;

internal class CallbackObjectTransition : TransitionAction
{
    public override GeneratedExpression TransitValue(string valueExpression, TransitionKind transitionKind,
        MethodGenerationUnit generationUnit)
    {
        return generationUnit switch {
            DownstreamMethodGenerationUnit => transitionKind switch {
                TransitionKind.JavaToJni => $"{valueExpression}.getHandle()",
                TransitionKind.JniToNative => $"reinterpret_cast<void*>({valueExpression})",
                _ => throw new UnsupportedJniScenarioException()
            },
            UpstreamMethodGenerationUnit => transitionKind switch {
                TransitionKind.NativeToJni => $"{JniInternalNames.CallbackLambdaContext}->globalObjectRef",
                TransitionKind.JniToJava => valueExpression,
                _ => throw new UnsupportedJniScenarioException()
            },
            _ => throw new UnsupportedJniScenarioException()
        };
    }
}
