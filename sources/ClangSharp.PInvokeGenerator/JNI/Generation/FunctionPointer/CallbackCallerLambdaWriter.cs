// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using ClangSharp.JNI.Generation.Method;
using ClangSharp.JNI.Generation.Transitions;
using ClangSharp.JNI.JNIGlue;

namespace ClangSharp.JNI.Generation.FunctionPointer;

internal static class CallbackCallerLambdaWriter
{
    public static void Write(IIndentedWriter writer, UpstreamMethodGenerationUnit upstreamMethodGen)
    {
        var lambda = upstreamMethodGen.JniCallbackCallerLambda;

        writer.Write("[]");
        writer.RawBuilder.AppendMethodParameters(lambda.Parameters, p => $"{p.Type.AsRawString} {p.Name}");
        writer.Write(" -> ");
        writer.Write(lambda.ReturnType);
        writer.WriteBlockStart();

        JniGlueTransitionWriter.Write(writer, upstreamMethodGen, TransitionKind.NativeToJni);

        writer.WriteBlockEnd();
    }
}
