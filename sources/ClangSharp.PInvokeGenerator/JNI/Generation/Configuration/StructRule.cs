// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

#nullable enable
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace ClangSharp.JNI.Generation.Configuration;

public record StructRule : JniGenerationConfigurationElement
{
    public static readonly StructRule Default = new();

    private Regex? _compiledNamePattern;
    [XmlAttribute] public string? NamePattern { get; init; }

    [XmlElement] public string? NameOverride { get; init; }

    [XmlElement] public bool? Exclude { get; init; }
    public bool ShouldBeGenerated => !Exclude ?? true;

    [XmlElement] public bool? GenerateLayoutMeta { get; init; }
    public bool ShouldGenerateLayoutMeta => GenerateLayoutMeta ?? false;

    public bool MatchesName(string name)
    {
        return RegexPatterns.CompileAndMatch(ref _compiledNamePattern, NamePattern, name);
    }

    public StructRule FusionWith(StructRule other)
    {
        return new StructRule {
            NamePattern = other.NamePattern ?? NamePattern,
            NameOverride = other.NameOverride ?? NameOverride,
            Exclude = other.Exclude ?? Exclude,
            GenerateLayoutMeta = other.GenerateLayoutMeta ?? GenerateLayoutMeta
        };
    }
}
