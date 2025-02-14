﻿// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ClangSharp.JNI.Generation.Transitions;

#nullable enable
namespace ClangSharp.JNI.Generation.Method;

internal abstract class MethodGenerationUnit : GenerationUnit
{
    protected MethodGenerationUnit(NativeOperation nativeOperation,
        MethodReturnValueLinkage? returnValueLinkage,
        IEnumerable<MethodParameterLinkage> parameterLinkages)
    {
        NativeOperation = nativeOperation;
        ReturnValueLinkage = returnValueLinkage;
        ParameterLinkages = parameterLinkages.ToImmutableArray();

        AllParameters = ExtractAllParameters();
    }

    public NativeOperation NativeOperation { get; }

    public MethodReturnValueLinkage? ReturnValueLinkage { get; }
    public ImmutableArray<MethodParameterLinkage> ParameterLinkages { get; }
    public ImmutableArray<TransitingMethodParameter> AllParameters { get; }

    private ImmutableArray<TransitingMethodParameter> ExtractAllParameters()
    {
        var builder = ImmutableArray.CreateBuilder<TransitingMethodParameter>(
            ParameterLinkages.Length + ReturnValueLinkage?.GeneratedParameters.Count ?? 0);

        foreach (var linkage in ParameterLinkages)
        {
            builder.AddRange(linkage.GeneratedParameters);
        }

        if (ReturnValueLinkage?.GeneratedParameters is { } returnParams)
        {
            builder.AddRange(returnParams);
        }

        return builder.Capacity == builder.Count ? builder.MoveToImmutable() : builder.ToImmutableArray();
    }

    protected ImmutableArray<MethodParameter<TTargetParam>> FilterParameters<TTargetParam>(
        Func<TransitingMethodParameter, MethodParameter<TTargetParam>?> parameterCaster)
    {
        var builder = ImmutableArray.CreateBuilder<MethodParameter<TTargetParam>>(AllParameters.Length);
        foreach (var parameter in AllParameters)
        {
            if (parameterCaster(parameter) is { } castedParameter)
            {
                builder.Add(castedParameter);
            }
        }

        return builder.ToImmutable();
    }

    public ImmutableArray<TransitingMethodParameter> GetSortedTransitingParameters(TransitionKind transitionKind)
    {
        return AllParameters
            .Where(parameter => parameter.TransitionBehaviors.Supports(transitionKind))
            .OrderBy(x => x.IntermediateOrdering)
            .ToImmutableArray();
    }
}
