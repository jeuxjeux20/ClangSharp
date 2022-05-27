// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Collections.Immutable;
using ClangSharp.JNI.Generation.Method;

namespace ClangSharp.JNI.Generation.FunctionPointer;

internal class FunctionPointerTransformationUnit : TransformationUnit<FunctionPointerTarget>
{
    public CallbackInterfaceGenerationUnit InterfaceGenerationUnit { get; }
    public UpstreamMethodGenerationUnit MethodGenerationUnit { get; }

    public FunctionPointerTransformationUnit(FunctionPointerTarget target,
        JniGenerationContext context,
        out ImmutableArray<TransformationUnit> generatedTransformationUnits) : base(target)
    {
        InterfaceGenerationUnit = new CallbackInterfaceGenerationUnit(target, context);

        var operation = new RunCallbackFinalOperation(target.Type, InterfaceGenerationUnit.JavaType);

        MethodGenerationUnit = UpstreamMethodGenerationUnit.UseLinker(
            new UpstreamMethodLinker(operation, context),
            new UpstreamMethodGenerationProperties(InterfaceGenerationUnit.JavaType, context.Namings),
            out generatedTransformationUnits);
    }
}
