// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using ClangSharp.JNI.Java;

namespace ClangSharp.JNI.Generation.FunctionPointer;

internal class CallbackInterfaceGenerationUnit : GenerationUnit
{
    public string Name { get; }

    public ObjectJavaType JavaType { get; }

    public CallbackInterfaceGenerationUnit(FunctionPointerTarget target, JniGenerationContext context)
    {
        Name = $"Callback_{target.MethodName}_{target.ArgName}";
        JavaType = context.NestedTypeInContainer(Name);
    }
}
