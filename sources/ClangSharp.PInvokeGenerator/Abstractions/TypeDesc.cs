// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using ClangSharp.Interop;

namespace ClangSharp.Abstractions
{
    /// <summary>
    /// Contains a subset of data from a <see cref="Type"/>.
    /// </summary>
    internal abstract class TypeDesc
    {
        public static TypeDesc Create(Type cType)
        {
            TypeDesc type = cType switch {
                BuiltinType builtinType => new BuiltinTypeDesc(builtinType),
                PointerType pointerType => new PointerTypeDesc(pointerType),
                RecordType recordType => new RecordTypeDesc(recordType),
                FunctionProtoType functionProtoType => new FunctionProtoTypeDesc(functionProtoType),
                EnumType enumType => new EnumTypeDesc(enumType),
                // ElaboratedType elaboratedType => new ElaboratedTypeDesc(elaboratedType),
                _ => null
            };

            return type is not null && type.IsFullyFormed ? type : null;
        }

        protected TypeDesc(Type cType)
        {
            AsString = cType.AsString;
            Kind = cType.Kind;
        }

        protected bool IsFullyFormed { get; set; } = true;

        public string AsString { get; }
        public CXTypeKind Kind { get; }
        public override string ToString() => AsString;
    }

    internal sealed class BuiltinTypeDesc : TypeDesc
    {
        public BuiltinTypeDesc(BuiltinType cType) : base(cType)
        {
        }
    }
    internal sealed class PointerTypeDesc : TypeDesc
    {
        public TypeDesc PointeeType { get; }

        public PointerTypeDesc(PointerType cType) : base(cType)
        {
            PointeeType = Create(cType.PointeeType);

            IsFullyFormed = PointeeType != null;
        }
    }
    internal sealed class RecordTypeDesc : TypeDesc
    {
        public string Name { get; }

        public RecordTypeDesc(RecordType cType) : base(cType)
        {
            Name = cType.AsString;
        }
    }
    internal sealed class FunctionProtoTypeDesc : TypeDesc
    {
        public TypeDesc ReturnType { get; }
        public IReadOnlyList<TypeDesc> Parameters { get; }

        public FunctionProtoTypeDesc(FunctionProtoType cType) : base(cType)
        {
            ReturnType = Create(cType.ReturnType);
            Parameters = cType.ParamTypes.Select(Create).ToList();

            IsFullyFormed = ReturnType != null && Parameters.All(x => x is not null);
        }
    }

    internal sealed class EnumTypeDesc : TypeDesc
    {
        public string Name { get; }

        public EnumTypeDesc(EnumType cType) : base(cType)
        {
            Name = cType.AsString;
        }
    }
}
