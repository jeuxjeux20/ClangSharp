// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Text;
using ClangSharp.JNI.Java;
using ClangSharp.JNI.JNIGlue;

namespace ClangSharp.JNI
{
    internal static class JavaConventions
    {
        public const string JniEnvVariable = "env";

        public static string EscapeName(string name)
        {
            // This is a dirty hack to avoid C# escapes.
            if (name.StartsWith('@'))
            {
                name = name[1..];
            }

            // For some reason, the classic switch is disallowed because of the editorconfig.
            return name switch {
                "abstract" or "continue" or "for" or "new" or
                    "switch" or "assert" or "default" or "goto" or
                    "package" or "synchronized" or "boolean" or "do" or
                    "if" or "private" or "this" or "break" or
                    "double" or "implements" or "protected" or "throw" or
                    "byte" or "else" or "import" or "public" or
                    "throws" or "case" or "enum" or "instanceof" or
                    "return" or "transient" or "catch" or "extends" or
                    "int" or "short" or "try" or "char" or
                    "final" or "interface" or "static" or "void" or
                    "class" or "finally" or "long" or "strictfp" or
                    "volatile" or "const" or "float" or "native" or
                    "super" or "while" or "true" or "false" or "null" => name + "_",
                _ => name,
            };
        }

        public static string JniPackageName(string @namespace) => @namespace.Replace(".", "/");

        public static string Getter(string variableName, string cOrJavaType) => cOrJavaType switch {
            "bool" or "jboolean" or "boolean" => "is",
            _ => "get"
        } + UppercaseFirstLetter(variableName);

        public static string Setter(string variableName) => "set" + UppercaseFirstLetter(variableName);

        public static string ToScreamingCase(string value)
        {
            // Add some more capacity to avoid resizing the string.
            // 8 seems a good fit, UnlessYouHaveThisKindOfClassName.
            var builder = new StringBuilder(value.Length + 8);
            var currentBuilderIndex = 0;
            for (var i = 0; i < value.Length; i++)
            {
                var character = value[i];
                if (i != 0 && i != value.Length - 1)
                {
                    var nextCharacter = value[i + 1];
                    var previousCharacter = value[i - 1];
                    if (char.IsUpper(character) && (char.IsLower(nextCharacter) || char.IsLower(previousCharacter)))
                    {
                        _ = builder.Insert(currentBuilderIndex, "_");
                        currentBuilderIndex++;
                    }
                }

                _ = builder.Append(char.ToUpper(character));
                currentBuilderIndex++;
            }

            return builder.ToString();
        }

        private static string UppercaseFirstLetter(string value)
        {
            if (value.Length == 0)
            {
                return value;
            }

            return char.ToUpperInvariant(value[0]) + value[1..];
        }

        public static string JniProxyMethodName(ObjectJavaType containingType, string name)
        {
            return
                $"Java_{MangleNameForJniMethod(containingType.Package)}_{MangleNameForJniMethod(containingType.RawName)}_{MangleNameForJniMethod(name)}";
        }

        private static string MangleNameForJniMethod(string name)
        {
            // https://docs.oracle.com/javase/1.5.0/docs/guide/jni/spec/design.html#wp133
            var builder = new StringBuilder(name.Length + 16);
            foreach (var character in name)
            {
                if (character is
                        (>= 'a' and <= 'z') or
                        (>= 'A' and <= 'Z') ||
                    char.IsDigit(character))
                {
                    _ = builder.Append(character);
                }
                else
                {
                    _ = builder.Append(character switch {
                        '.' => "_",
                        '_' => "_1",
                        ';' => "_2",
                        '[' => "_3",
                        '$' => "_00024", // Commonly used, avoids a costly AppendFormat call.
                        _ => $"_0{(int)character:X4}"
                    });
                }
            }

            return builder.ToString();
        }
    }
}
