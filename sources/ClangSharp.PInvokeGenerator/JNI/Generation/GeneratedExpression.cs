// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

#nullable enable
using System;
using System.Diagnostics;

namespace ClangSharp.JNI.Generation;

internal readonly struct GeneratedExpression
{
    public string? StringValue { get; }
    public Action<IIndentedWriter>? WriterActionValue { get; }

    public GeneratedExpression(string? stringValue)
    {
        StringValue = stringValue;
        WriterActionValue = null;
    }

    public GeneratedExpression(Action<IIndentedWriter>? writerActionValue)
    {
        StringValue = null;
        WriterActionValue = writerActionValue;
    }

    public void WriteTo(IIndentedWriter writer)
    {
        if (StringValue is not null)
        {
            writer.Write(StringValue);
        }
        else
        {
            WriterActionValue!.Invoke(writer);
        }
    }

    public static implicit operator GeneratedExpression(string stringValue) => new(stringValue);
}
