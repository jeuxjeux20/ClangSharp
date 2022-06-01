// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using ClangSharp.JNI.Generation.Method;

namespace ClangSharp.JNI;

internal static class JniStringBuilderExtensions
{
    public static StringBuilder AppendTemplateString(this StringBuilder builder, string value)
    {
        builder.Append("FumoCement::TemplateString<");
        for (var i = 0; i < value.Length; i++)
        {
            var character = value[i];
            builder.Append('\'');
            builder.Append(character == '\'' ? @"\'" : character.ToString());
            builder.Append('\'');
            if (i != value.Length - 1)
            {
                builder.Append(", ");
            }
        }

        builder.Append(">");
        return builder;
    }

    public static StringBuilder AppendMethodCallExpression<T>(this StringBuilder builder, string name,
        IReadOnlyList<T> arguments, Func<T, string> toString, bool removeOpeningParenthesis = false)
    {
        builder.Append(name);
        if (!removeOpeningParenthesis)
        {
            builder.Append('(');
        }

        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i];
            builder.Append(toString(argument));
            if (i != arguments.Count - 1)
            {
                builder.Append(", ");
            }
        }
        builder.Append(')');
        return builder;
    }

    public static StringBuilder AppendMethodParameters<T>(this StringBuilder builder, IReadOnlyList<T> parameters,
        Func<T, string> toString, bool removeOpeningParenthesis = false)
    {
        return AppendMethodCallExpression(builder, "", parameters, toString, removeOpeningParenthesis);
    }
}
