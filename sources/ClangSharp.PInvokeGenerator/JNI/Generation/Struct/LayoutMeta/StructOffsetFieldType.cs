// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

#nullable enable
using ClangSharp.Abstractions;

namespace ClangSharp.JNI.Generation.Struct.LayoutMeta;

internal abstract record StructOffsetFieldType
{
    public static StructOffsetFieldType Create(TypeDesc type, JniGenerationContext context)
    {
        return (StructOffsetFieldType?) ScalarStructOffsetFieldType.FromNativeType(type) ??
               RecordStructOffsetFieldType.FromNativeType(type, context) ??
               throw new UnsupportedJniScenarioException("Incompatible struct field type for an offset field.");
    }

    public abstract string OffsetValueExpression { get; }
}
