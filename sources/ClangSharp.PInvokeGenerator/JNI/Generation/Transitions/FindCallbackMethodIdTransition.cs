// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using ClangSharp.JNI.Generation.Method;

namespace ClangSharp.JNI.Generation.Transitions;

internal class FindCallbackMethodIdTransition : ValueTransition
{
    public override GeneratedExpression TransitValue(string valueExpression, TransitionKind transitionKind,
        MethodGenerationUnit generationUnit)
    {
        throw new UnsupportedJniScenarioException();
    }

    public override GeneratedExpression GenerateValue(TransitionKind transitionKind, MethodGenerationUnit generationUnit)
    {
        if (transitionKind is not TransitionKind.NativeToJni ||
            generationUnit is not UpstreamMethodGenerationUnit upstreamMethodGen)
        {
            throw new UnsupportedJniScenarioException();
        }

        void Write(IIndentedWriter writer)
        {
            writer.Write("FumoCement::getCachedStaticMethod<");
            writer.RawBuilder.AppendTemplateString(upstreamMethodGen.CallbackType.FullJniClass);
            writer.Write(", ");
            writer.RawBuilder.AppendTemplateString(upstreamMethodGen.CallbackCallerMethod.Name);
            writer.Write(", ");
            writer.RawBuilder.AppendTemplateString(upstreamMethodGen.CallbackCallerMethod.JniSignature);
            writer.Write(">()");
        }

        return new GeneratedExpression(Write);
    }
}
