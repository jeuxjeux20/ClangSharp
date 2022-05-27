// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Collections.Immutable;
using ClangSharp.Abstractions;
using ClangSharp.JNI.Generation.Method;

namespace ClangSharp.JNI.Generation.Struct;

internal record GetStructFieldOperation(RecordTypeDesc StructType, TypeDesc FieldType, string FieldName)
    : NativeOperation(FieldType,
        ImmutableArray.Create<NativeOperationParameter>(new StructHandleParameter(StructType)))
{
    public override string GenerateRunExpression(MethodGenerationUnit generationUnit)
    {
        var structParameterLinkage = generationUnit.ParameterLinkages[0];
        return $"{structParameterLinkage.TransitingParameter.IntermediateName}->{FieldName}";
    }
}
