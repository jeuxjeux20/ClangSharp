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

        protected TypeDesc(Type canonicalType, Type rawType) : this(canonicalType.AsString, canonicalType.Kind,
            rawType?.AsString)
        {
        }

        protected TypeDesc(string stringRepr, CXTypeKind kind, string rawStringRepr = null)
        {
            AsString = stringRepr;
            AsRawString = rawStringRepr ?? stringRepr;
            Kind = kind;
        }

        protected bool IsFullyFormed { get; set; } = true;
        /// <summary>
        /// The original representation of that type, without "sugared" types, especially typedefs.
        /// For example, <c>SomeStruct*</c> might become <c>SomeStructHandle</c>.
        /// </summary>
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
        public static readonly BuiltinTypeDesc Void = new(CXTypeKind.CXType_Void, "void");

        public BuiltinTypeDesc(BuiltinType canonicalType, Type rawType) : base(canonicalType, rawType)
        {
        }

        private BuiltinTypeDesc(CXTypeKind kind, string repr) : base(repr, kind)
        {
        }
    }

    internal sealed class PointerTypeDesc : TypeDesc
    {
        public static readonly PointerTypeDesc VoidPointer = new(BuiltinTypeDesc.Void);

        public TypeDesc PointeeType { get; }

        public PointerTypeDesc(PointerType canonicalType, Type rawType) : base(canonicalType, rawType)
        {
            var rawPointerType = RecoverRawType<PointerType>(rawType);
            PointeeType = Create(canonicalType.PointeeType, rawPointerType?.PointeeType);

            IsFullyFormed = PointeeType != null;
        }

        public PointerTypeDesc(TypeDesc pointeeType) : base(pointeeType.AsString + "*", CXTypeKind.CXType_Pointer,
            pointeeType.AsRawString != null ? pointeeType.AsRawString + "*" : null)
        {
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

        public RecordTypeDesc(string recordName) : base(recordName, CXTypeKind.CXType_Record)
        {
            Name = recordName;
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
