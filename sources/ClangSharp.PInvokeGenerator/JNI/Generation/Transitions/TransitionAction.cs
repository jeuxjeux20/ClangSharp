// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

#nullable enable
using ClangSharp.JNI.Generation.Method;

namespace ClangSharp.JNI.Generation.Transitions;

internal abstract class TransitionAction
{
    public virtual bool NeedsIntermediateReturnValue => false;
    public virtual bool NeedsCppToCTransformation => false;

    public abstract GeneratedExpression TransitValue(string valueExpression,
        TransitionKind transitionKind, MethodGenerationUnit generationUnit);

    public virtual GeneratedExpression GenerateValue(TransitionKind transitionKind, MethodGenerationUnit generationUnit)
    {
        throw new UnsupportedJniScenarioException("Cannot generate a value with this transition.");
    }
}
