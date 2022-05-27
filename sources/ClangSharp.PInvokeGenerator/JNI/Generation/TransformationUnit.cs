// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

#nullable enable
namespace ClangSharp.JNI.Generation;

internal abstract class TransformationUnit
{
    protected TransformationUnit(TransformationTarget target)
    {
        Target = target;
    }

    public TransformationTarget Target { get; }
}

internal abstract class TransformationUnit<T> : TransformationUnit where T : TransformationTarget
{
    protected TransformationUnit(T target) : base(target)
    {
    }

    public new T Target => (T)base.Target;
}
