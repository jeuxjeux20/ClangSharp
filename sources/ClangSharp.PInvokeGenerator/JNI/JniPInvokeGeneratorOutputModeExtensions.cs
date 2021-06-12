// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

namespace ClangSharp.JNI
{
    public static class JniPInvokeGeneratorOutputModeExtensions
    {
        public static bool IsJniRelated(this PInvokeGeneratorOutputMode mode)
            => mode is PInvokeGeneratorOutputMode.JavaClasses or PInvokeGeneratorOutputMode.JniGlue;
    }
}
