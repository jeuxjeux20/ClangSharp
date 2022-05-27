// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace ClangSharp.JNI.Generation.Enum;

internal readonly struct EnumConstant
{
    public static EnumConstant Unsigned(string name, ulong? unsignedValue)
    {
        return new(name, unsignedValue, null);
    }

    public static EnumConstant Signed(string name, long? signedValue)
    {
        return new(name, null, signedValue);
    }

    public string JavaName { get; }
    public ulong? UnsignedValue { get; }
    public long? SignedValue { get; }

    public bool IsSigned => SignedValue is not null;
    public bool IsUnsigned => UnsignedValue is not null;

    private EnumConstant(string javaName, ulong? unsignedValue, long? signedValue)
    {
        JavaName = javaName;
        UnsignedValue = unsignedValue;
        SignedValue = signedValue;
    }
}
