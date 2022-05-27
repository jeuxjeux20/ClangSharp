// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System.Text;
using ClangSharp.JNI.Generation;

namespace ClangSharp.JNI;

internal interface IIndentedWriter
{
    StringBuilder RawBuilder { get; }
    void Write<T>(T value);
    void Write(GeneratedExpression generatedExpression);
    void WriteIndentation();
    void WriteIndentedLine(string value = "");
    void WriteNewLine();
    void DecreaseIndentation();
    void IncreaseIndentation();
    void WriteBlockStart();
    void WriteBlockEnd();
}
