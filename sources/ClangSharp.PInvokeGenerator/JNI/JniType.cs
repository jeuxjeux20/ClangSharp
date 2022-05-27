// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using ClangSharp.Abstractions;
using ClangSharp.JNI.Java;

#nullable enable
namespace ClangSharp.JNI
{
    internal readonly struct JniType : IEquatable<JniType>
    {
        public static readonly JniType Void = new("void");

        public static readonly JniType JObject = new("jobject");
        public static readonly JniType JClass = new("jclass");
        public static readonly JniType JString = new("jstring");

        public static readonly JniType JBoolean = new("jboolean");
        public static readonly JniType JByte = new("jbyte");
        public static readonly JniType JChar = new("jchar");
        public static readonly JniType JShort = new("jshort");
        public static readonly JniType JInt = new("jint");
        public static readonly JniType JLong = new("jlong");
        public static readonly JniType JFloat = new("jfloat");
        public static readonly JniType JDouble = new("jdouble");

        public static readonly JniType JArray = new("jarray");
        public static readonly JniType JObjectArray = new("jobjectArray");
        public static readonly JniType JBooleanArray = new("jbooleanArray");
        public static readonly JniType JByteArray = new("jbyteArray");
        public static readonly JniType JCharArray = new("jcharArray");
        public static readonly JniType JShortArray = new("jshortArray");
        public static readonly JniType JIntArray = new("jintArray");
        public static readonly JniType JLongArray = new("jlongArray");
        public static readonly JniType JFloatArray = new("jfloatArray");
        public static readonly JniType JDoubleArray = new("jdoubleArray");

        public static readonly JniType JThrowable = new("jthrowable");

        public static readonly JniType JClassId = new("jclassID");
        public static readonly JniType JMethodId = new("jmethodID");

        private JniType(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static implicit operator string(JniType type) => type.Value;

        public override string ToString() => Value;

        private static readonly Dictionary<JniType, JavaType> s_jniToJava = new() {
            [Void] = JavaType.Void,
            [JBoolean] = JavaType.Boolean,
            [JByte] = JavaType.Byte,
            [JChar] = JavaType.Char,
            [JShort] = JavaType.Short,
            [JInt] = JavaType.Int,
            [JLong] = JavaType.Long,
            [JFloat] = JavaType.Float,
            [JDouble] = JavaType.Double,
            [JObjectArray] = JavaType.ObjectArray,
            [JBooleanArray] = JavaType.BooleanArray,
            [JByteArray] = JavaType.ByteArray,
            [JCharArray] = JavaType.CharArray,
            [JShortArray] = JavaType.ShortArray,
            [JIntArray] = JavaType.IntArray,
            [JLongArray] = JavaType.LongArray,
            [JFloatArray] = JavaType.FloatArray,
            [JDoubleArray] = JavaType.DoubleArray,
            [JClass] = JavaType.Class,
            [JString] = JavaType.String,
        };

        public JavaType AsJavaNonObject()
        {
            if (!s_jniToJava.TryGetValue(this, out var javaType))
            {
                throw new InvalidOperationException($"Couldn't convert JNI (non-object) type to Java: '{Value}'.");
            }

            return javaType;
        }

        public JavaType? AsJava()
        {
            return s_jniToJava.GetValueOrDefault(this);
        }

        // RecordTypeDesc isn't the best type to use, but being precise doesn't really matter here.
        public TypeDesc AsNative() => new RecordTypeDesc(Value);

        public bool Equals(JniType other)
            => string.Equals(Value, other.Value, StringComparison.InvariantCulture);

        public override bool Equals(object? obj) => obj is JniType other && Equals(other);

        public override int GetHashCode() => (Value != null ? StringComparer.InvariantCulture.GetHashCode(Value) : 0);

        public static bool operator ==(JniType left, JniType right) => left.Equals(right);

        public static bool operator !=(JniType left, JniType right) => !left.Equals(right);
    }
}
