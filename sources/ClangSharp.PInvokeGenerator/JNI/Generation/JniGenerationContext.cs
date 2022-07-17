// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Collections.Generic;
using System.Linq;
using ClangSharp.JNI.Generation.Configuration;
using ClangSharp.JNI.Java;

namespace ClangSharp.JNI.Generation;
#nullable enable
internal class JniGenerationContext
{
    public string Package { get; set; }
    public JniGenerationConfiguration Configuration { get; }
    public string ContainerClass { get; set; }

    public ObjectJavaType ContainerType { get; }

    private readonly List<TransformationUnit> _transformationUnits = new();
    public IReadOnlyList<TransformationUnit> TransformationUnits => _transformationUnits;


    private readonly List<TransformationUnit> _roundTransformationUnits = new();
    public IReadOnlyList<TransformationUnit> RoundTransformationUnits => _roundTransformationUnits;

    public JniGenerationContext(string package, JniGenerationConfiguration configuration,
        string containerClass = "Native")
    {
        Package = package;
        Configuration = configuration;
        ContainerClass = containerClass;
        ContainerType = new ObjectJavaType(Package, ContainerClass);
    }

    public void AddTransformationUnit(TransformationUnit unit)
    {
        _transformationUnits.Add(unit);
        _roundTransformationUnits.Add(unit);
    }

    public void AddTransformationUnits(IReadOnlyCollection<TransformationUnit> units)
    {
        _transformationUnits.AddRange(units);
        _roundTransformationUnits.AddRange(units);
    }

    public void NewRound()
    {
        _roundTransformationUnits.Clear();
    }

    public IEnumerable<T> GetTransformationUnits<T>() where T : TransformationUnit
        => TransformationUnits.OfType<T>();

    public IEnumerable<T> GetRoundTransformationUnits<T>() where T : TransformationUnit
        => RoundTransformationUnits.OfType<T>();

    public ObjectJavaType NestedTypeInContainer(string name)
        => new(Package, $"{ContainerClass}.{name}");
}
