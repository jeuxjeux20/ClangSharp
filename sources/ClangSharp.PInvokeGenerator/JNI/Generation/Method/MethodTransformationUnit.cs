// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

#nullable enable
using System.Collections.Immutable;
using ClangSharp.JNI.Generation.Configuration;

namespace ClangSharp.JNI.Generation.Method;

/// <summary>
/// Transforms a native method into a set of Java and JNI methods to call it.
/// </summary>
internal sealed class MethodTransformationUnit : TransformationUnit<MethodTarget>
{
    public DownstreamMethodGenerationUnit MethodGenerationUnit { get; }

    public MethodTransformationUnit(MethodTarget target, MethodRule rule, JniGenerationContext context,
        out ImmutableArray<TransformationUnit> generatedTransformationUnits) : base(target)
    {
        var operation = new CallNativeMethodOperation(target.Method);
        var methodName = rule.NameOverride ?? target.Method.Name;

        MethodGenerationUnit = DownstreamMethodGenerationUnit.UseLinker(
            new DownstreamMethodLinker(operation, context),
            new DownstreamMethodGenerationProperties(methodName, methodName + "Raw",
                context.ContainerType, rule.ShouldExposeRawMethod),
            out generatedTransformationUnits);
    }
}
