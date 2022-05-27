// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Collections.Generic;
using System.Linq;
using ClangSharp.JNI.Java;

namespace ClangSharp.JNI.Generation;
#nullable enable
internal class JniGenerationContext
{
    public string Package { get; set; }
    public string ContainerClass { get; set; }

    public ObjectJavaType ContainerType { get; }

    private readonly List<TransformationUnit> _transformationUnits = new();
    public IReadOnlyList<TransformationUnit> TransformationUnits => _transformationUnits;

    public JniGenerationNamings Namings { get; }

    public JniGenerationContext(string package, string containerClass = "Native", JniGenerationNamings? namings = null)
    {
        Package = package;
        ContainerClass = containerClass;
        ContainerType = new ObjectJavaType(Package, ContainerClass);
        Namings = namings ?? JniGenerationNamings.Default;
    }

    public void AddTransformationUnit(TransformationUnit unit)
    {
        _transformationUnits.Add(unit);
    }

    public void AddTransformationUnits(IEnumerable<TransformationUnit> units)
    {
        _transformationUnits.AddRange(units);
    }

    public IEnumerable<T> GetTransformationUnits<T>() where T : TransformationUnit
        => TransformationUnits.OfType<T>();

    public ObjectJavaType NestedTypeInContainer(string name)
        => new(Package, $"{ContainerClass}.{name}");

    public ObjectJavaType StructTypeInContainer(string name)
        => new(Package, $"{ContainerClass}.{StructTypeName(name)}");

    public static string StructTypeName(string name)
        => $"{name}";
}
