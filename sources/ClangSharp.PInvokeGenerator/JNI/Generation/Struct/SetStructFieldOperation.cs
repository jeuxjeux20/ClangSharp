// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Collections.Immutable;
using ClangSharp.Abstractions;
using ClangSharp.JNI.Generation.Method;

namespace ClangSharp.JNI.Generation.Struct;

internal record SetStructFieldOperation(RecordTypeDesc StructType, TypeDesc FieldType, string FieldName)
    : NativeOperation(BuiltinTypeDesc.Void,
        ImmutableArray.Create(
            new StructHandleParameter(StructType),
            new NativeOperationParameter(FieldType, "newValue")))
{
    public override string GenerateRunExpression(MethodGenerationUnit generationUnit)
    {
        var structParameter = generationUnit.ParameterLinkages[0].TransitingParameter.IntermediateName;
        var newValueParameter = generationUnit.ParameterLinkages[1].TransitingParameter.IntermediateName;
        return $"{structParameter}->{FieldName} = FumoCement::passAsC({newValueParameter})";
    }
}
