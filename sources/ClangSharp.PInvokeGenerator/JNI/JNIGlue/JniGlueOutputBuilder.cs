// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClangSharp.JNI.Generation;
using ClangSharp.JNI.Generation.FunctionPointer;
using ClangSharp.JNI.Generation.Method;
using ClangSharp.JNI.Generation.Struct;
using ClangSharp.JNI.Generation.Transitions;
using ClangSharp.JNI.Java;

namespace ClangSharp.JNI.JNIGlue
{
    internal sealed class JniGlueOutputBuilder : JniOutputBuilderBase
    {
        public JniGlueOutputBuilder(string name,
            PInvokeGeneratorConfiguration configuration,
            JniGenerationContext generationContext,
            string indentationString = DefaultIndentationString)
            : base(name, configuration, generationContext, indentationString)
        {
        }

        public override bool IsTestOutput => false;
        public override string Extension => ".h";

        protected override void WriteContent()
        {
            foreach (var structUnit in GenerationContext.GetRoundTransformationUnits<StructTransformationUnit>())
            {
                var @struct = structUnit.Target;
                WriteIndentedLine($"// Struct declaration: {@struct.NativeName}");

                WriteStructClassMethods(structUnit.ClassGenerationUnit);

                foreach (var fieldAccessor in structUnit.FieldAccessorGenerationUnits)
                {
                    WriteMethod(fieldAccessor);
                }

                WriteIndentedLine($"// End struct declaration: {@struct.NativeName}");
            }

            foreach (var method in GenerationContext.GetRoundTransformationUnits<MethodTransformationUnit>())
            {
                WriteMethod(method.MethodGenerationUnit);
            }
        }

        private void WriteStructClassMethods(StructClassGenerationUnit structGen)
        {
            WriteAllocateStructMethod(structGen);
            WriteDestroyStructMethod(structGen);
            WriteOverwriteMethod(structGen);
        }

        private void WriteAllocateStructMethod(StructClassGenerationUnit structGen)
        {
            BeginJniMethod(JniType.JLong,
                JavaConventions.JniProxyMethodName(structGen.JavaStructType,
                    StructClassGenerationUnit.AllocateStructMethodName),
                true,
                Array.Empty<MethodParameter<JniType>>());

            WriteIndentedLine($"return FumoCement::toJavaPointer(new {structGen.NativeName});");

            EndJniMethod();
        }

        private void WriteDestroyStructMethod(StructClassGenerationUnit structGen)
        {
            BeginJniMethod(JniType.Void,
                JavaConventions.JniProxyMethodName(structGen.JavaStructType,
                    StructClassGenerationUnit.DestroyStructMethodName),
                true,
                new MethodParameter<JniType>[] { new(JniType.JLong, "handle") });

            WriteIndentedLine($"delete FumoCement::toNativePointer<{structGen.NativeName}>(handle);");

            EndJniMethod();
        }

        private void WriteOverwriteMethod(StructClassGenerationUnit structGen)
        {
            BeginJniMethod(JniType.Void,
                JavaConventions.JniProxyMethodName(structGen.JavaStructType,
                    StructClassGenerationUnit.OverwriteMethodName),
                true,
                new MethodParameter<JniType>[] {
                    new(JniType.JLong, "targetHandle"), new(JniType.JLong, "dataHandle")
                });

            WriteIndentedLine($"*FumoCement::toNativePointer<{structGen.NativeName}>(targetHandle) = " +
                              $"*FumoCement::toNativePointer<{structGen.NativeName}>(dataHandle);");

            EndJniMethod();
        }

        private void WriteMethod(DownstreamMethodGenerationUnit methodGen)
        {
            var nativeMethod = methodGen.JniProxyMethod;

            BeginJniMethod(nativeMethod);

            JniGlueTransitionWriter.Write(this, methodGen, TransitionKind.JniToNative);

            EndJniMethod();
        }

        // Base constructs

        private void BeginJniMethod(JniGlueMethod method)
        {
            BeginJniMethod(method.ReturnType.Value,
                method.Name,
                true,
                method.Parameters);
        }

        private void BeginJniMethod(string returnType, string jniMethodName, bool isStatic,
            IEnumerable<MethodParameter<JniType>> arguments)
        {
            WriteIndentedLine("JNIEXPORT ");
            Write(returnType);
            Write(" JNICALL ");

            Write(jniMethodName);

            Write("(JNIEnv* env, ");
            Write(isStatic ? "jclass" : "jobject");

            foreach (var arg in arguments)
            {
                Write(", ");
                Write(arg.Type);
                Write(" ");
                Write(arg.Name);
            }

            Write(")");

            WriteBlockStart();
        }

        private void EndJniMethod() => WriteBlockEnd();
    }
}
