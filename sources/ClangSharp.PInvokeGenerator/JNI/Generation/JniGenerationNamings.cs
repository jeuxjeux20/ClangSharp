// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace ClangSharp.JNI.Generation;

internal sealed record JniGenerationNamings
{
    public static readonly JniGenerationNamings Default = new();

    // Callbacks
    public string CallbackInterfaceMethod { get; init; } = "execute";
    public string CallbackInterfaceCallerMethod { get; init; } = "runCallback";

    public static class Internal
    {
        public const string CallbackLambdaJEnv = "func$$context";
        public const string ReturnValueIntermediate = "returnValue$int";
    }
}
