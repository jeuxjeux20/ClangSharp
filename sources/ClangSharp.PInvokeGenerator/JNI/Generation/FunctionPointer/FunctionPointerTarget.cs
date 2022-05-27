// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using ClangSharp.Abstractions;
using ClangSharp.JNI.Generation.Method;
using ClangSharp.JNI.Generation.Struct;

namespace ClangSharp.JNI.Generation.FunctionPointer;

internal record FunctionPointerTarget(string MethodName, string ArgName, FunctionProtoTypeDesc Type)
    : TransformationTarget
{
    public static FunctionPointerTarget FromFinalOperation(FinalOperation operation, FinalOperationParameter parameter)
    {
        var methodName = operation switch {
            CallNativeMethodFinalOperation methodOperation => methodOperation.Method.Name,
            GetStructFieldOperation getOperation => $"{getOperation.StructType.Name}_{getOperation.FieldName}Get",
            SetStructFieldOperation setOperation => $"{setOperation.StructType.Name}_{setOperation.FieldName}Set",
            _ => operation.GetType().Name
        };
        var argName = parameter.Name;
        var type = (FunctionProtoTypeDesc)((PointerTypeDesc)parameter.Type).PointeeType;

        return new FunctionPointerTarget(methodName, argName, type);
    }
}
