// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using ClangSharp.Interop;
using ClangSharp.JNI;

#nullable enable
namespace ClangSharp.Abstractions
{
    /// <summary>
    /// Contains a subset of data from a canonical <see cref="Type"/>.
    /// </summary>
    internal abstract class TypeDesc
        : IEquatable<TypeDesc>
    {
        public static TypeDesc? CreateOptional(Type type,
            TypeDescCreationMethod creationMethod = TypeDescCreationMethod.ConvertToCanonical,
            TypeDesc? verbatim = null,
            bool isSubType = false)
        {
            var typeToTransform = type;

            if (creationMethod == TypeDescCreationMethod.ConvertToCanonical)
            {
                typeToTransform = type.CanonicalType;
                if (typeToTransform == type) // Canonical and original are the same!
                {
                    // We can't really tell if both types are verbatim in a sub type,
                    // as we might get entirely different types from canonical to verbatim.
                    // This will be specified later using DetermineVerbatim.

                    var method = isSubType
                        ? TypeDescCreationMethod.IsAlreadyCanonical
                        : TypeDescCreationMethod.IsVerbatim;

                    return CreateOptional(typeToTransform, method, isSubType: isSubType);
                }

                var convertedTypeDesc = CreateOptional(typeToTransform,
                    creationMethod: TypeDescCreationMethod.IsAlreadyCanonical,
                    verbatim: CreateOptional(type, TypeDescCreationMethod.IsVerbatim, isSubType: isSubType));
                return convertedTypeDesc;
            }
            else
            {
                var verbatimStatus = creationMethod switch {
                    TypeDescCreationMethod.IsAlreadyCanonical
                        => isSubType ? VerbatimStatus.Undetermined : VerbatimStatus.NonVerbatim,
                    TypeDescCreationMethod.IsVerbatim => VerbatimStatus.Verbatim,
                    _ => VerbatimStatus.Undetermined
                };

                return typeToTransform switch {
                    BuiltinType builtinType => new BuiltinTypeDesc(builtinType, verbatimStatus, verbatim),
                    PointerType pointerType => new PointerTypeDesc(pointerType, verbatimStatus, verbatim),
                    RecordType recordType => new RecordTypeDesc(recordType, verbatimStatus, verbatim),
                    FunctionProtoType functionProtoType
                        => new FunctionProtoTypeDesc(functionProtoType, verbatimStatus, verbatim),
                    EnumType enumType => new EnumTypeDesc(enumType, verbatimStatus, verbatim),
                    ElaboratedType elaboratedType => CreateOptional(elaboratedType.NamedType,
                        VerbatimElseConvertCanonical(verbatimStatus),
                        verbatim),
                    TypedefType typeDef => new TypedefTypeDesc(typeDef, verbatimStatus, verbatim),
                    _ => null
                };
            }
        }

        public static TypeDesc Create(Type type,
            TypeDescCreationMethod creationMethod = TypeDescCreationMethod.ConvertToCanonical,
            bool isSubType = false)
        {
            return CreateOptional(type, creationMethod, isSubType: isSubType) ??
                   throw new UnsupportedJniScenarioException($"Cannot transform type {type}.");
        }

        public static TypeDescCreationMethod VerbatimElseConvertCanonical(VerbatimStatus verbatimStatus)
            => verbatimStatus == VerbatimStatus.Verbatim
                ? TypeDescCreationMethod.IsVerbatim
                : TypeDescCreationMethod.ConvertToCanonical;

        private bool _propagatedVerbatims = false;

        protected TypeDesc(Type canonicalType, VerbatimStatus verbatimStatus, TypeDesc? verbatimType) : this(
            canonicalType.AsString,
            canonicalType.Kind,
            verbatimType)
        {
            VerbatimStatus = verbatimStatus;
        }

        protected TypeDesc(string stringRepr, CXTypeKind kind, TypeDesc? verbatimType)
        {
            AsString = stringRepr;
            VerbatimType = verbatimType;
            Kind = kind;
        }

        protected TypeDesc CreateSubType(Type type)
        {
            return Create(type, VerbatimElseConvertCanonical(VerbatimStatus), isSubType: true);
        }

        /// <summary>
        /// The original representation of that type, without "sugared" types, especially typedefs.
        /// For example, <c>SomeStruct*</c> might become <c>SomeStructHandle</c>.
        /// </summary>
        public TypeDesc? VerbatimType { get; private set; }

        public VerbatimStatus VerbatimStatus { get; private set; } = VerbatimStatus.Undetermined;

        public string AsVerbatimString => VerbatimType?.AsString ?? AsString;
        public string AsString { get; }
        public CXTypeKind Kind { get; }
        public override string ToString() => AsVerbatimString;

        internal void DetermineVerbatimStatus(TypeDesc? verbatim)
        {
            if (VerbatimStatus == VerbatimStatus.Undetermined && verbatim is not null)
            {
                if (verbatim == this)
                {
                    VerbatimStatus = VerbatimStatus.Verbatim;
                }
                else
                {
                    VerbatimStatus = VerbatimStatus.NonVerbatim;
                    VerbatimType = verbatim;
                }
            }
            if (!_propagatedVerbatims && verbatim is not null)
            {
                PropagateVerbatimDeterminations(verbatim);
                _propagatedVerbatims = true;
            }
        }

        protected void DetermineVerbatimForType<T>(TypeDesc? verbatim) where T : TypeDesc
        {
            DetermineVerbatimStatus(verbatim as T ?? (verbatim as TypedefTypeDesc)?.DefinedType as T);
        }

        protected virtual void PropagateVerbatimDeterminations(TypeDesc verbatim)
        {
        }

        public bool Equals(TypeDesc? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return AsString == other.AsString && Kind == other.Kind;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((TypeDesc)obj);
        }

        public override int GetHashCode() => HashCode.Combine(AsString, (int)Kind);

        public static bool operator ==(TypeDesc? left, TypeDesc? right) => Equals(left, right);

        public static bool operator !=(TypeDesc? left, TypeDesc? right) => !Equals(left, right);

        public static TypedefTypeDesc? ResolveUntilLastTypeDef(TypeDesc? type)
        {
            // We don't want to resolve the very last typedef.
            while (type is TypedefTypeDesc { DefinedType: TypedefTypeDesc } typeDesc)
            {
                type = typeDesc.DefinedType;
            }

            return type as TypedefTypeDesc;
        }
    }

    internal sealed class BuiltinTypeDesc : TypeDesc
    {
        public static readonly BuiltinTypeDesc Void = new(CXTypeKind.CXType_Void, "void");

        public BuiltinTypeDesc(BuiltinType canonicalType, VerbatimStatus verbatimStatus, TypeDesc? verbatimType) : base(
            canonicalType,
            verbatimStatus, verbatimType)
        {
        }

        private BuiltinTypeDesc(CXTypeKind kind, string repr) : base(repr, kind, null)
        {
        }
    }

    internal sealed class PointerTypeDesc : TypeDesc
    {
        public static readonly PointerTypeDesc VoidPointer = new(BuiltinTypeDesc.Void);

        public TypeDesc PointeeType { get; }

        public PointerTypeDesc(PointerType canonicalType, VerbatimStatus verbatimStatus, TypeDesc? verbatimType) : base(
            canonicalType,
            verbatimStatus, verbatimType)
        {
            PointeeType = CreateSubType(canonicalType.PointeeType);

            DetermineVerbatimForType<PointerTypeDesc>(verbatimType);
        }

        public PointerTypeDesc(TypeDesc pointeeType) : base(pointeeType.AsString + "*", CXTypeKind.CXType_Pointer,
            null)
        {
            PointeeType = pointeeType;
        }

        protected override void PropagateVerbatimDeterminations(TypeDesc verbatim)
        {
            base.PropagateVerbatimDeterminations(verbatim);

            if (verbatim is PointerTypeDesc pointerVerbatim)
            {
                PointeeType.DetermineVerbatimStatus(pointerVerbatim.PointeeType);
            }
        }
    }

    internal sealed class RecordTypeDesc : TypeDesc
    {
        public string Name { get; }

        public RecordTypeDesc(RecordType canonicalType, VerbatimStatus verbatimStatus, TypeDesc? verbatimType) : base(
            canonicalType,
            verbatimStatus, verbatimType)
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

            DetermineVerbatimForType<RecordTypeDesc>(verbatimType);
        }

        public RecordTypeDesc(string recordName) : base(recordName, CXTypeKind.CXType_Record, null)
        {
            Name = recordName;
        }
    }

    internal sealed class FunctionProtoTypeDesc : TypeDesc
    {
        public TypeDesc ReturnType { get; }
        public IReadOnlyList<TypeDesc> Parameters { get; }

        public FunctionProtoTypeDesc(FunctionProtoType canonicalType, VerbatimStatus verbatimStatus,
            TypeDesc? verbatimType) : base(
            canonicalType, verbatimStatus,
            verbatimType)
        {
            ReturnType = CreateSubType(canonicalType.ReturnType);
            Parameters = canonicalType.ParamTypes.Select(CreateSubType).ToList();

            DetermineVerbatimForType<FunctionProtoTypeDesc>(verbatimType);
        }

        protected override void PropagateVerbatimDeterminations(TypeDesc verbatim)
        {
            if (verbatim is FunctionProtoTypeDesc functionVerbatim)
            {
                ReturnType.DetermineVerbatimStatus(functionVerbatim.ReturnType);
                for (var i = 0; i < Parameters.Count; i++)
                {
                    Parameters[i].DetermineVerbatimStatus(functionVerbatim.Parameters[i]);
                }
            }
        }
    }

    internal sealed class EnumTypeDesc : TypeDesc
    {
        public string Name { get; }

        public EnumTypeDesc(EnumType canonicalType, VerbatimStatus verbatimStatus, TypeDesc? verbatimType) : base(
            canonicalType,
            verbatimStatus, verbatimType)
        {
            Name = canonicalType.AsString;

            DetermineVerbatimForType<EnumTypeDesc>(verbatimType);
        }
    }

    internal sealed class TypedefTypeDesc : TypeDesc
    {
        public TypeDesc DefinedType { get; }

        public TypedefTypeDesc(TypedefType canonicalType, VerbatimStatus verbatimStatus, TypeDesc? verbatimType) : base(
            canonicalType,
            verbatimStatus, verbatimType)
        {
            DefinedType = CreateSubType(canonicalType.Decl.UnderlyingType);

            DetermineVerbatimForType<TypedefTypeDesc>(verbatimType);
        }
    }

    public enum TypeDescCreationMethod
    {
        ConvertToCanonical,
        IsAlreadyCanonical,
        IsVerbatim
    }

    public enum VerbatimStatus
    {
        Verbatim,
        NonVerbatim,
        Undetermined
    }
}
