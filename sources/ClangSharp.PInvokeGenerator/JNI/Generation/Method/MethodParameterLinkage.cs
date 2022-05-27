// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ClangSharp.JNI.Generation.Method;

internal class MethodParameterLinkage
{
    public MethodParameterLinkage(FinalOperationParameter targetParameter,
        TransitingMethodParameter transitingParameter,
        ImmutableArray<TransitingMethodParameter> extraParameters)
    {
        TargetParameter = targetParameter;
        TransitingParameter = transitingParameter;
        GeneratedParameters = extraParameters.Insert(0, transitingParameter);
    }

    public FinalOperationParameter TargetParameter { get; }
    public TransitingMethodParameter TransitingParameter { get; }
    public ImmutableArray<TransitingMethodParameter> GeneratedParameters { get; }
}
