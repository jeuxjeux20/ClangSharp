// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Linq;
using ClangSharp.Abstractions;
using ClangSharp.JNI.Generation.Configuration;

namespace ClangSharp.JNI.Generation.Struct.LayoutMeta;

#nullable enable
internal record RecordStructOffsetFieldType : StructOffsetFieldType
{
    public RecordStructOffsetFieldType(string javaStructName, JniNamings namings)
    {
        JavaStructName = javaStructName;
        OffsetValueExpression =
            $"{JniInternalNames.StructArrangerField}.addField({javaStructName}.{namings.StructMetaLayoutField})";
    }

    public override string OffsetValueExpression { get; }

    public string JavaStructName { get; init; }

    public static RecordStructOffsetFieldType? FromNativeType(TypeDesc type, JniGenerationContext context)
    {
        if (type is RecordTypeDesc recordType)
        {
            var record = context.GetTransformationUnits<StructTransformationUnit>()
                .FirstOrDefault(x => x.Target.NativeStructType == recordType);

            if (record?.LayoutMetaGenerationUnit is null)
            {
                throw new UnsupportedJniScenarioException(
                    $"Can't resolve field offsets for struct {recordType.Name}. " +
                    "Make sure it is not excluded and that its metadata generation is enabled.");
            }

            return new RecordStructOffsetFieldType(record.ClassGenerationUnit.JavaName, context.Configuration.Namings);
        }

        return null;
    }
}
