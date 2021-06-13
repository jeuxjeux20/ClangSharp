// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ClangSharp.JNI.Java
{
    internal abstract class JavaType
    {
        public static readonly BuiltinJavaType Void = new(JavaTypeKind.Void);
        public static readonly BuiltinJavaType Boolean = new(JavaTypeKind.Boolean);
        public static readonly BuiltinJavaType Byte = new(JavaTypeKind.Byte);
        public static readonly BuiltinJavaType Char = new(JavaTypeKind.Char);
        public static readonly BuiltinJavaType Short = new(JavaTypeKind.Short);
        public static readonly BuiltinJavaType Int = new(JavaTypeKind.Int);
        public static readonly BuiltinJavaType Long = new(JavaTypeKind.Long);
        public static readonly BuiltinJavaType Float = new(JavaTypeKind.Float);
        public static readonly BuiltinJavaType Double = new(JavaTypeKind.Double);

        public static readonly ObjectJavaType Object = new("java.lang", "Object");
        public static readonly ObjectJavaType String = new("java.lang", "String");
        public static readonly ObjectJavaType Class = new("java.lang", "Class", new[] { "?" });

        public static readonly ArrayJavaType ObjectArray = new(Object);
        public static readonly ArrayJavaType BooleanArray = new(Boolean);
        public static readonly ArrayJavaType ByteArray = new(Byte);
        public static readonly ArrayJavaType CharArray = new(Char);
        public static readonly ArrayJavaType ShortArray = new(Short);
        public static readonly ArrayJavaType IntArray = new(Int);
        public static readonly ArrayJavaType LongArray = new(Long);
        public static readonly ArrayJavaType FloatArray = new(Float);
        public static readonly ArrayJavaType DoubleArray = new(Double);

        private readonly string[] _annotations;
        public IReadOnlyList<string> Annotations => _annotations;

        protected JavaType()
        {
            _annotations = Array.Empty<string>();
        }

        protected JavaType(IEnumerable<string> annotations)
        {
            _annotations = annotations.ToArray();
        }

        public abstract JavaTypeKind Kind { get; }
        protected abstract string Repr { get; }

        public string AsString => string.Join(" ", _annotations) + (_annotations.Any() ? " " : "") + Repr;
        public string AsStringNoAnnotations => Repr;

        public abstract string JniTypeSignature { get; }

        public override string ToString() => AsString;

        public abstract JavaType WithAddedAnnotations(params string[] annotations);
    }

    internal sealed class BuiltinJavaType : JavaType
    {
        internal BuiltinJavaType(JavaTypeKind kind)
        {
            Kind = kind;
            Repr = kind switch {
                JavaTypeKind.Boolean => "boolean",
                JavaTypeKind.Byte => "byte",
                JavaTypeKind.Char => "char",
                JavaTypeKind.Short => "short",
                JavaTypeKind.Int => "int",
                JavaTypeKind.Long => "long",
                JavaTypeKind.Float => "float",
                JavaTypeKind.Double => "double",
                JavaTypeKind.Void => "void",
                _ => throw new ArgumentException("Kind is not a builtin type.", nameof(kind))
            };
        }

        private BuiltinJavaType(JavaTypeKind kind, string stringRepr, IEnumerable<string> annotations)
            : base(annotations)
        {
            Kind = kind;
            Repr = stringRepr;
        }

        public override JavaTypeKind Kind { get; }
        protected override string Repr { get; }
        public override string JniTypeSignature => Kind switch {
            JavaTypeKind.Boolean => "Z",
            JavaTypeKind.Byte => "B",
            JavaTypeKind.Char => "C",
            JavaTypeKind.Short => "S",
            JavaTypeKind.Int => "I",
            JavaTypeKind.Long => "J",
            JavaTypeKind.Float => "F",
            JavaTypeKind.Double => "D",
            JavaTypeKind.Void => "V",
            _ => throw new InvalidOperationException("No JNI type signature, this shouldn't happen!")
        };

        public override BuiltinJavaType WithAddedAnnotations(params string[] annotations)
            => new(Kind, Repr, Annotations.Concat(annotations));
    }

    internal sealed class ObjectJavaType : JavaType
    {
        private readonly string[] _genericParameters;

        public ObjectJavaType(string package, string name, IEnumerable<string> genericParameters = null)
        {
            Package = package;
            Name = name;
            _genericParameters = genericParameters?.ToArray() ?? Array.Empty<string>();

            Repr = Package + "." + Name +
                   (_genericParameters.Length > 0 ? "<" + string.Join(", ", _genericParameters) + ">" : "");

            RawName = Name.Replace(".", "$");
            FullJniClass = $"{JavaConventions.JniPackageName(Package)}/{RawName}";
            JniTypeSignature = $"L{FullJniClass};";
        }

        private ObjectJavaType(string package, string name, string rawName, string fullyQualifiedName,
            string jniTypeSignature,
            string fullJniClass,
            string[] genericParameters,
            IEnumerable<string> annotations) : base(annotations)
        {
            Package = package;
            Name = name;
            RawName = rawName;
            Repr = fullyQualifiedName;
            FullJniClass = fullJniClass;
            JniTypeSignature = jniTypeSignature;
            _genericParameters = genericParameters;
        }

        public override JavaTypeKind Kind => JavaTypeKind.Object;
        protected override string Repr { get; }
        public override string JniTypeSignature { get; }

        public string FullJniClass { get; }

        public string Package { get; }
        public string Name { get; }
        public string RawName { get; }

        public IReadOnlyList<string> GenericParameters => _genericParameters;

        public override ObjectJavaType WithAddedAnnotations(params string[] annotations) =>
            new(Package, Name, RawName, Repr, JniTypeSignature, FullJniClass, _genericParameters,
                Annotations.Concat(annotations));
    }

    internal sealed class ArrayJavaType : JavaType
    {
        public ArrayJavaType(JavaType elementType)
        {
            if (elementType.Kind == JavaTypeKind.Void)
            {
                throw new ArgumentException("Element type is void.", nameof(elementType));
            }

            ElementType = elementType;
            Repr = elementType.AsString + "[]";
            JniTypeSignature = "[" + elementType.JniTypeSignature;
        }

        public override JavaTypeKind Kind => JavaTypeKind.Array;
        protected override string Repr { get; }
        public override string JniTypeSignature { get; }
        public JavaType ElementType { get; }

        private ArrayJavaType(string repr, string jniTypeSignature, JavaType elementType,
            IEnumerable<string> annotations) : base(annotations)
        {
            Repr = repr;
            JniTypeSignature = jniTypeSignature;
            ElementType = elementType;
        }

        public override ArrayJavaType WithAddedAnnotations(params string[] annotations) =>
            new(Repr, JniTypeSignature, ElementType, Annotations.Concat(annotations));
    }

    internal enum JavaTypeKind
    {
        Void,
        Boolean,
        Byte,
        Char,
        Short,
        Int,
        Long,
        Float,
        Double,
        Object,
        Array
    }
}
