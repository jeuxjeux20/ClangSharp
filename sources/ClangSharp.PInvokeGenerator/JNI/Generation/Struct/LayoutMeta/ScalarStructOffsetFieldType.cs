// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using ClangSharp.Abstractions;
using ClangSharp.Interop;

namespace ClangSharp.JNI.Generation.Struct.LayoutMeta;

#nullable enable
internal record ScalarStructOffsetFieldType(ScalarStructOffsetFieldType.Kind ScalarType) : StructOffsetFieldType
{
    public enum Kind
    {
        Bool,
        Char,
        Short,
        Int,
        Long,
        LongLong,
        Float,
        Double,
        Pointer,
        FixedInt8,
        FixedInt16,
        FixedInt32,
        FixedInt64
    }

    public static ScalarStructOffsetFieldType? FromNativeType(TypeDesc type)
    {
        if (type.IsPointerLikeType())
        {
            return new ScalarStructOffsetFieldType(Kind.Pointer);
        }

        if (type.IsFixedWidthIntegerType(out _, out var bits, out _))
        {
            var kind = bits switch {
                8 => Kind.FixedInt8,
                16 => Kind.FixedInt16,
                32 => Kind.FixedInt32,
                64 => Kind.FixedInt64,
                _ => throw new UnsupportedJniScenarioException($"Unsupported amount of bits: {bits}")
            };
            return new ScalarStructOffsetFieldType(kind);
        }

        if (type is BuiltinTypeDesc { Kind: var cKind })
        {
            var scalarKind = cKind switch {
                CXTypeKind.CXType_Bool => Kind.Bool,

                CXTypeKind.CXType_Char_U or
                    CXTypeKind.CXType_UChar or
                    CXTypeKind.CXType_SChar or
                    CXTypeKind.CXType_Char_S => Kind.Char,

                CXTypeKind.CXType_Short or
                    CXTypeKind.CXType_UShort => Kind.Short,

                CXTypeKind.CXType_Int or
                    CXTypeKind.CXType_UInt => Kind.Int,

                CXTypeKind.CXType_Long or
                    CXTypeKind.CXType_ULong => Kind.Long,

                CXTypeKind.CXType_LongLong or
                    CXTypeKind.CXType_ULongLong => Kind.LongLong,

                CXTypeKind.CXType_Float => Kind.Float,
                CXTypeKind.CXType_Double => Kind.Double,
                _ => throw new UnsupportedJniScenarioException("Unsupported scalar type for layout field.")
            };
            return new ScalarStructOffsetFieldType(scalarKind);
        }

        return null;
    }

    public override string OffsetValueExpression { get; } =
        $"{JniInternalNames.StructArrangerField}.{FindArrangerCall(ScalarType)}";

    private static string FindArrangerCall(Kind scalarType)
    {
        return scalarType switch {
            Kind.Bool => "addCBoolField()",
            Kind.Char => "addCCharField()",
            Kind.Short => "addCShortField()",
            Kind.Int => "addCIntField()",
            Kind.Long => "addCLongField()",
            Kind.LongLong => "addCLongLongField()",
            Kind.Float => "addCFloatField()",
            Kind.Double => "addCDoubleField()",
            Kind.Pointer => "addCPointerField()",
            Kind.FixedInt8 => "addFixedField(1)",
            Kind.FixedInt16 => "addFixedField(2)",
            Kind.FixedInt32 => "addFixedField(4)",
            Kind.FixedInt64 => "addFixedField(8)",
            _ => throw new ArgumentException("Unknown scalar type.")
        };
    }
}
