// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ClangSharp.JNI.Generation.Method;
using ClangSharp.JNI.Generation.Struct;

namespace ClangSharp.JNI.Generation.Configuration;
#nullable enable

[XmlRoot(Namespace = "https://novelrt.org/jni-generation-config")]
public class JniGenerationConfiguration
{
    public static readonly JniGenerationConfiguration Default = new();

    private Dictionary<string, MethodRule> _methodRuleLookup = new();
    private Dictionary<string, StructRule> _structRuleLookup = new();

    [XmlElement(typeof(MethodRule), ElementName = "MethodRule")]
    [XmlElement(typeof(StructRule), ElementName = "StructRule")]
    public List<JniGenerationConfigurationElement> Elements { get; init; } = new();

    public JniNamings Namings { get; init; } = JniNamings.Default;

    internal MethodRule GetMethodRule(string methodName)
    {
        if (_methodRuleLookup.TryGetValue(methodName, out var methodRule))
        {
            return methodRule;
        }

        methodRule = Elements
            .OfType<MethodRule>()
            .Where(x => x.MatchesName(methodName))
            .Aggregate(MethodRule.Default, (a, b) => a.FusionWith(b));

        return _methodRuleLookup[methodName] = methodRule;
    }

    internal MethodRule GetMethodRule(MethodTarget method) => GetMethodRule(method.Method.Name);

    internal StructRule GetStructRule(string structName)
    {
        if (_structRuleLookup.TryGetValue(structName, out var structRule))
        {
            return structRule;
        }

        structRule = Elements
            .OfType<StructRule>()
            .Where(x => x.MatchesName(structName))
            .Aggregate(StructRule.Default, (a, b) => a.FusionWith(b));

        return _structRuleLookup[structName] = structRule;
    }

    internal StructRule GetStructRule(StructTarget @struct) => GetStructRule(@struct.NativeName);
}
