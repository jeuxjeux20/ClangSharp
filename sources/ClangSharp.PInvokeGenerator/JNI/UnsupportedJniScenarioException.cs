// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;

namespace ClangSharp.JNI
{
    internal class UnsupportedJniScenarioException : Exception
    {
        public static UnsupportedJniScenarioException UnsupportedType<T>(T type)
            => new($"Type not supported: {type}.");

        public static UnsupportedJniScenarioException UnsupportedPass(string message, ValuePass valuePass)
            => new($"Cannot handle {valuePass}: {message}");

        public static UnsupportedJniScenarioException UnsupportedPass(ValuePass valuePass)
            => new($"Cannot handle {valuePass} in this scenario.");

        public UnsupportedJniScenarioException()
        {
        }

        public UnsupportedJniScenarioException(string message) : base(message)
        {
        }

        public UnsupportedJniScenarioException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
