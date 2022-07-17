// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

#nullable enable
using System.Diagnostics.CodeAnalysis;
using ClangSharp.Abstractions;

namespace ClangSharp.JNI;

internal static class JniTypeDescExtensions
{
    public static bool IsPointerLikeType(this TypeDesc? type, out string? typeName)
    {
        if (type is null)
        {
            typeName = null;
            return false;
        }

        if (type is PointerTypeDesc)
        {
            typeName = null;
            return true;
        }

        if (TypeDesc.ResolveUntilLastTypeDef(type) is
            { AsString: "size_t" or "usize_t" or "intptr_t" or "uintptr_t" } foundType)
        {
            typeName = foundType.AsString;
            return true;
        }

        return IsPointerLikeType(type.VerbatimType, out typeName);
    }

    public static bool IsPointerLikeType(this TypeDesc? type)
    {
        return IsPointerLikeType(type, out _);
    }

    // https://en.cppreference.com/w/c/types/integer
    public static bool IsFixedWidthIntegerType(this TypeDesc? type, [NotNullWhen(true)] out string? typeName,
        out int bits, out bool unsigned)
    {
        if (TypeDesc.ResolveUntilLastTypeDef(type) is { AsString: var name })
        {
            var foundBits = name switch {
                "int8_t" or "uint8_t" => 8,
                "int16_t" or "uint16_t" => 16,
                "int32_t" or "uint32_t" => 32,
                "int64_t" or "uint64_t" => 64,
                _ => 0
            };

            if (foundBits != 0)
            {
                typeName = name;
                bits = foundBits;
                unsigned = name.StartsWith('u');
                return true;
            }
            else
            {
                type = null; // Invalidate the type.
            }
        }

        if (type is not null)
        {
            return IsFixedWidthIntegerType(type.VerbatimType, out typeName, out bits, out unsigned);
        }
        else
        {
            typeName = null;
            bits = 0;
            unsigned = false;
            return false;
        }
    }
}
