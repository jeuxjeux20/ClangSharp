// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            string indentationString = DefaultIndentationString) : base(name,
            configuration, indentationString)
        {
        }

        public override bool IsTestOutput => false;
        public override string Extension => ".h";

        protected override void WriteContent()
        {
            foreach (var structUnit in GenerationContext.GetTransformationUnits<StructTransformationUnit>())
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

            foreach (var method in GenerationContext.GetTransformationUnits<MethodTransformationUnit>())
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
            foreach (var parameter in methodGen.GetSortedTransitingParameters(TransitionKind.JniToNative))
            {
                var intermediateName = parameter.IntermediateName;

                WriteIndentedLine($"auto&& {intermediateName} = ");
                Write(parameter.TransitOrGenerateValue(TransitionKind.JniToNative, methodGen));
                Write(';');
            }

            // Call the method (and put the return value in a variable if any)
            WriteIndentedLine();
            if (methodGen.ReturnValueLinkage is not null)
            {
                Write("return ");
            }

            var expression = methodGen.NativeOperation.GenerateRunExpression(methodGen);
            if (methodGen.ReturnValueLinkage is not null)
            {
                Write(methodGen.ReturnValueLinkage.TransitValue(expression, TransitionKind.NativeToJni, methodGen));
            }
            else
            {
                Write(expression);
            }

            Write(";");

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
