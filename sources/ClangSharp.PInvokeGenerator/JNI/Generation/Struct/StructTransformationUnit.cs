// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using ClangSharp.Abstractions;
using ClangSharp.JNI.Generation.Configuration;
using ClangSharp.JNI.Generation.Method;
using ClangSharp.JNI.Generation.Struct.LayoutMeta;
using ClangSharp.JNI.Generation.Transitions;
using ClangSharp.JNI.Java;

namespace ClangSharp.JNI.Generation.Struct;
#nullable enable
internal class StructTransformationUnit : TransformationUnit<StructTarget>
{
    public StructClassGenerationUnit ClassGenerationUnit { get; }
    public StructLayoutMetaGenerationUnit? LayoutMetaGenerationUnit { get; }
    public List<DownstreamMethodGenerationUnit> FieldAccessorGenerationUnits { get; } = new();

    public StructTransformationUnit(StructTarget target, StructRule rule, JniGenerationContext context,
        out ImmutableArray<TransformationUnit> generatedTransformationUnits) : base(target)
    {
        ClassGenerationUnit = new StructClassGenerationUnit(target, context, rule);
        if (rule.ShouldGenerateLayoutMeta)
        {
            LayoutMetaGenerationUnit = new StructLayoutMetaGenerationUnit(target, context);
        }

        var transformationUnitsBuilder = ImmutableArray.CreateBuilder<TransformationUnit>();
        foreach (var (fieldType, fieldName) in target.Fields)
        {
            var getOp = new GetStructFieldOperation(target.NativeStructType, fieldType, fieldName);
            var setOp = new SetStructFieldOperation(target.NativeStructType, fieldType, fieldName);
            var getterName = JavaConventions.Getter(fieldName, fieldType.AsVerbatimString);
            if (getterName == "getHandle") // Avoid conflicts with FumoCement methods
            {
                getterName = "getHandleField";
            }

            var setterName = JavaConventions.Setter(fieldName);

            var getGen = DownstreamMethodGenerationUnit.UseLinker(
                new DownstreamMethodLinker(getOp, context),
                new DownstreamMethodGenerationProperties(getterName, getterName + "Raw",
                    ClassGenerationUnit.JavaStructType, ExposeRawMethod: true, IsJavaInstanceMethod: true),
                out var getTransformUnits);
            transformationUnitsBuilder.AddRange(getTransformUnits);

            var setGen = DownstreamMethodGenerationUnit.UseLinker(
                new DownstreamMethodLinker(setOp, context),
                new DownstreamMethodGenerationProperties(setterName, setterName + "Raw",
                    ClassGenerationUnit.JavaStructType, ExposeRawMethod: true, IsJavaInstanceMethod: true),
                out var setTransformUnits);
            transformationUnitsBuilder.AddRange(setTransformUnits);

            FieldAccessorGenerationUnits.Add(getGen);
            FieldAccessorGenerationUnits.Add(setGen);
        }

        generatedTransformationUnits = transformationUnitsBuilder.ToImmutable();
    }
}
