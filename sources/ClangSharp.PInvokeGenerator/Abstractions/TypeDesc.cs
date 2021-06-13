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
        public static TypeDesc Create(Type canonicalType, Type rawType)
        {
            TypeDesc type = canonicalType switch {
                BuiltinType builtinType => new BuiltinTypeDesc(builtinType, rawType),
                PointerType pointerType => new PointerTypeDesc(pointerType, rawType),
                RecordType recordType => new RecordTypeDesc(recordType, rawType),
                FunctionProtoType functionProtoType => new FunctionProtoTypeDesc(functionProtoType, rawType),
                EnumType enumType => new EnumTypeDesc(enumType, rawType),
                // ElaboratedType elaboratedType => new ElaboratedTypeDesc(elaboratedType),
                _ => null
            };

            if (type is null || !type.IsFullyFormed)
            {
                return type;
            }

            type.AsRawString = rawType?.AsString ?? type.AsString;

            return type;
        }

        protected TypeDesc(Type canonicalType, Type rawType)
        {
            AsString = canonicalType.AsString;
            AsRawString = rawType?.AsString ?? AsString;
            Kind = canonicalType.Kind;
        }

        protected bool IsFullyFormed { get; set; } = true;
        public string AsRawString { get; private set; }
        public string AsString { get; }
        public CXTypeKind Kind { get; }
        public override string ToString() => AsString;

        protected T RecoverRawType<T>(Type rawType) where T : Type
        {
            return rawType as T ?? (rawType as TypedefType)?.Decl?.UnderlyingType as T ?? rawType.Desugar as T;
        }
    }

    internal sealed class BuiltinTypeDesc : TypeDesc
    {
        public BuiltinTypeDesc(BuiltinType canonicalType, Type rawType) : base(canonicalType, rawType)
        {
        }
    }

    internal sealed class PointerTypeDesc : TypeDesc
    {
        public TypeDesc PointeeType { get; }

        public PointerTypeDesc(PointerType canonicalType, Type rawType) : base(canonicalType, rawType)
        {
            var rawPointerType = RecoverRawType<PointerType>(rawType);
            PointeeType = Create(canonicalType.PointeeType, rawPointerType?.PointeeType);

            IsFullyFormed = PointeeType != null;
        }
    }

    internal sealed class RecordTypeDesc : TypeDesc
    {
        public string Name { get; }

        public RecordTypeDesc(RecordType canonicalType, Type rawType) : base(canonicalType, rawType)
        {
            if (canonicalType.IsLocalConstQualified)
            {
                // This is necessary because for some reason canonicalType.Record.Name returns null!
                Name = canonicalType.AsString[6..];
            }
            else
            {
                Name = canonicalType.AsString;
            }
        }
    }

    internal sealed class FunctionProtoTypeDesc : TypeDesc
    {
        public TypeDesc ReturnType { get; }
        public IReadOnlyList<TypeDesc> Parameters { get; }

        public FunctionProtoTypeDesc(FunctionProtoType canonicalType, Type rawType) : base(canonicalType, rawType)
        {
            var rawFunctionType = RecoverRawType<FunctionProtoType>(rawType);

            ReturnType = Create(canonicalType.ReturnType, rawFunctionType?.ReturnType);
            Parameters = canonicalType.ParamTypes
                .Select((x, i) => Create(x, rawFunctionType?.ParamTypes?[i]))
                .ToList();

            IsFullyFormed = ReturnType != null && Parameters.All(x => x is not null);
        }
    }

    internal sealed class EnumTypeDesc : TypeDesc
    {
        public string Name { get; }

        public EnumTypeDesc(EnumType canonicalType, Type rawType) : base(canonicalType, rawType)
        {
            Name = canonicalType.AsString;
        }
    }
}
