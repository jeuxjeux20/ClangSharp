// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ClangSharp.JNI.Generation.Enum;

internal class EnumTransformationUnit : TransformationUnit<EnumTarget>
{
    public EnumClassGenerationUnit ClassGenerationUnit { get; }

    public EnumTransformationUnit(EnumTarget target) : base(target)
    {
        ClassGenerationUnit = new EnumClassGenerationUnit(target);
    }
}

internal class EnumClassGenerationUnit : GenerationUnit
{
    public string JavaName { get; }
    public IReadOnlyList<EnumField> Fields { get; }

    public EnumClassGenerationUnit(EnumTarget target)
    {
        JavaName = JavaConventions.EscapeName(target.Name);
        Fields = target.Constants.Select(ConstantToField).ToArray();
    }

    private static EnumField ConstantToField(EnumConstant x)
    {
        var value = x.SignedValue?.ToString() ?? x.UnsignedValue?.ToString() ?? throw new InvalidOperationException();
        return new EnumField(x.IsSigned ? "int" : "@Unsigned int", x.JavaName, value);
    }
}
