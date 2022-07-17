// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

#nullable enable
using System.Text.RegularExpressions;

namespace ClangSharp.JNI.Generation.Configuration;

internal static class RegexPatterns
{
    public static bool CompileAndMatch(ref Regex? regex, string? pattern, string target)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return true;
        }

        regex ??= new Regex(pattern, RegexOptions.Compiled);
        return regex.IsMatch(target);
    }
}
