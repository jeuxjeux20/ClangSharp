// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

// Ported from https://github.com/llvm/llvm-project/tree/llvmorg-13.0.0/clang/include/clang-c
// Original source is Copyright (c) the LLVM Project and Contributors. Licensed under the Apache License v2.0 with LLVM Exceptions. See NOTICE.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace ClangSharp.Interop.UnitTests
{
    /// <summary>Provides validation of the <see cref="CXIdxContainerInfo" /> struct.</summary>
    public static unsafe class CXIdxContainerInfoTests
    {
        /// <summary>Validates that the <see cref="CXIdxContainerInfo" /> struct is blittable.</summary>
        [Test]
        public static void IsBlittableTest()
        {
            Assert.AreEqual(sizeof(CXIdxContainerInfo), Marshal.SizeOf<CXIdxContainerInfo>());
        }

        /// <summary>Validates that the <see cref="CXIdxContainerInfo" /> struct has the right <see cref="LayoutKind" />.</summary>
        [Test]
        public static void IsLayoutSequentialTest()
        {
            Assert.True(typeof(CXIdxContainerInfo).IsLayoutSequential);
        }

        /// <summary>Validates that the <see cref="CXIdxContainerInfo" /> struct has the correct size.</summary>
        [Test]
        public static void SizeOfTest()
        {
            if (Environment.Is64BitProcess)
            {
                Assert.AreEqual(32, sizeof(CXIdxContainerInfo));
            }
            else
            {
                Assert.AreEqual(20, sizeof(CXIdxContainerInfo));
            }
        }
    }
}
