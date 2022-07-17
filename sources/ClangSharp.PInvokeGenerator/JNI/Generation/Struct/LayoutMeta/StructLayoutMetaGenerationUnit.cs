// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

#nullable enable
using System.Collections.Immutable;

namespace ClangSharp.JNI.Generation.Struct.LayoutMeta;

internal class StructLayoutMetaGenerationUnit : GenerationUnit
{
    public ImmutableArray<StructOffsetField> OffsetFields { get; }

    public StructLayoutMetaGenerationUnit(StructTarget target, JniGenerationContext context)
    {
        var builder = ImmutableArray.CreateBuilder<StructOffsetField>(target.Fields.Length);
        foreach (var (type, name) in target.Fields)
        {
            var fieldType = StructOffsetFieldType.Create(type, context);
            var fieldName = string.Format(context.Configuration.Namings.StructMetaOffsetFieldFormat, name);
            builder.Add(new StructOffsetField(fieldType, fieldName));
        }

        OffsetFields = builder.MoveToImmutable();
    }
}
