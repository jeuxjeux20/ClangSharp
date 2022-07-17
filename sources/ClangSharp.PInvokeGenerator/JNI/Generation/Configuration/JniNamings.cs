// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace ClangSharp.JNI.Generation.Configuration;

public sealed record JniNamings
{
    public static readonly JniNamings Default = new();

    // Callbacks
    public string CallbackInterfaceMethod { get; init; } = "execute";
    public string CallbackInterfaceCallerMethod { get; init; } = "runCallback";

    public string StructMetaOffsetFieldFormat { get; init; } = "OFFSET_{0}";
    public string StructMetaLayoutField { get; init; } = "LAYOUT";
    public string StructMetaSizeField { get; init; } = "SIZE";
}
