// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Linq;
using ClangSharp.JNI.Generation.FunctionPointer;
using ClangSharp.JNI.Generation.Method;

namespace ClangSharp.JNI.Generation.Transitions;

internal class CallbackObjectTransition : ValueTransition
{
    public override GeneratedExpression TransitValue(string valueExpression, TransitionKind transitionKind,
        MethodGenerationUnit generationUnit)
    {
        return transitionKind switch {
            TransitionKind.JavaToJni => $"{valueExpression}.getHandle()",
            TransitionKind.JniToNative => $"reinterpret_cast<void*>({valueExpression})",
            _ => throw new UnsupportedJniScenarioException()
        };
    }

    public override GeneratedExpression GenerateValue(TransitionKind transitionKind,
        MethodGenerationUnit generationUnit)
    {
        if (transitionKind != TransitionKind.NativeToJni || generationUnit is not UpstreamMethodGenerationUnit)
        {
            throw new UnsupportedJniScenarioException();
        }

        var contextLinkage = generationUnit.ParameterLinkages.First(x => x.TargetParameter is CallbackContextParameter);
        return $"static_cast<FumoCement::FunctionPointerContext*>({contextLinkage.TransitingParameter.Name})" +
               "->globalObjectRef";
    }
}
