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
            foreach (var parameter in methodGen.GetTransitingParameters(TransitionKind.JniToNative))
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

        private void PassParametersInIntermediateVariables(IEnumerable<ValuePass> parameterPasses,
            Func<ValuePass, string, string> valuePasser,
            string envVariable = "env")
        {
            foreach (var parameterPass in parameterPasses)
            {
                WriteIndentedLine($"auto&& {parameterPass.IntermediateVariableName} = ");
                Write(valuePasser(parameterPass, envVariable));
                Write(';');
            }
        }

        private void PassIntermediateVariablesAsArgs(IReadOnlyList<ValuePass> parameterPasses)
        {
            for (var i = 0; i < parameterPasses.Count; i++)
            {
                var parameterPass = parameterPasses[i];

                // The passAsC method is important for C++ objects that use RAII.
                // For instance, we can't just pass an std::string as a const char*.
                Write("FumoCement::passAsC(");
                Write(parameterPass.IntermediateVariableName);
                Write(")");
                if (i != parameterPasses.Count - 1)
                {
                    Write(", ");
                }
            }
        }

        private void WriteFunctionPointerLambda(PassFunctionPointerProxyLambda proxyLambdaPass)
        {
            const string ClassNameAlias = "ClassName";
            const string MethodNameAlias = "MethodName";
            const string MethodSignatureAlias = "MethodSignature";

            var generationSet = proxyLambdaPass.LambdaProxyGenerationSet;

            var nativeLambda = generationSet.NativeLambda;
            var upstreamMethod = generationSet.UpstreamCallbackCallMethod;

            var returnPass = generationSet.UpstreamReturnValuePass;
            var parameterPasses = generationSet.UpstreamParameterPasses;

            var contextParameter = nativeLambda.Parameters[^1].Name;
            var castedContext = generationSet.CastedFuncContextVariable;

            BeginNativeLambda(generationSet.NativeLambda);

            WriteIndentedLine(
                $"auto* {castedContext} = static_cast<FumoCement::FunctionPointerContext*>({contextParameter});");
            WriteIndentedLine($"auto* funcEnv = {castedContext}->getEnv();");

            PassParametersInIntermediateVariables(parameterPasses, PassToJava, "funcEnv");

            WriteTemplateStringField(ClassNameAlias, proxyLambdaPass.CallbackCallClass);
            WriteTemplateStringField(MethodNameAlias, proxyLambdaPass.CallbackCallMethod);
            WriteTemplateStringField(MethodSignatureAlias, proxyLambdaPass.CallbackCallSignature);

            WriteIndentedLine($"auto classId = FumoCement::getCachedClass<{ClassNameAlias}>(funcEnv);");
            WriteIndentedLine($"auto methodId = FumoCement::getCachedStaticMethod<{ClassNameAlias}, " +
                              $"{MethodNameAlias}, " +
                              $"{MethodSignatureAlias}>(funcEnv);");

            var method = upstreamMethod.ReturnType switch {
                { Kind: JavaTypeKind.Void } => "CallStaticVoidMethod",
                { Kind: JavaTypeKind.Object or JavaTypeKind.Array } => "CallStaticObjectMethod",
                { Kind: JavaTypeKind.Boolean } => "CallStaticBoolMethod",
                { Kind: JavaTypeKind.Byte } => "CallStaticByteMethod",
                { Kind: JavaTypeKind.Char } => "CallStaticCharMethod",
                { Kind: JavaTypeKind.Short } => "CallStaticShortMethod",
                { Kind: JavaTypeKind.Int } => "CallStaticIntMethod",
                { Kind: JavaTypeKind.Long } => "CallStaticLongMethod",
                { Kind: JavaTypeKind.Float } => "CallStaticFloatMethod",
                { Kind: JavaTypeKind.Double } => "CallStaticDoubleMethod",
                _ => throw new UnsupportedJniScenarioException($"Cannot use return type: {upstreamMethod.ReturnType}")
            };

            if (returnPass is not null)
            {
                Write($"auto&& {returnPass.ValueToPass} = ");
            }

            WriteIndentedLine($"funcEnv->{method}(classId, methodId, ");
            PassIntermediateVariablesAsArgs(parameterPasses);
            Write(");");

            if (returnPass is not null)
            {
                WriteIndentedLine("return ");
                Write(PassToNative(returnPass, "funcEnv"));
                Write(";");
            }

            EndNativeLambda();
        }

        private static string PassToJava(ValuePass valuePass, string envVariable = "env")
        {
            return valuePass switch {
                PassPrimitive passPrimitive
                    => $"FumoCement::toJavaPrimitive({passPrimitive.ValueToPass})",

                PassPointerAsJLongToPtr passPointer
                    => $"FumoCement::toJavaPointer({passPointer.ValueToPass})",

                PassStringAsJByteArrayToCharPtr { RequiresDeletionEnum: true } passString
                    => $"FumoCement::toJavaStringBytes({envVariable}, {passString.ValueToPass}, {passString.DeletionEnumParameter})",

                PassStringAsJByteArrayToCharPtr { RequiresDeletionEnum: false } passString
                    => $"FumoCement::toJavaStringBytes({envVariable}, {passString.ValueToPass}, false)",

                PassNestedStructAsJLongPointerToStructPtr passNestedStruct
                    => $"FumoCement::toJavaPointer(&{passNestedStruct.ValueToPass})",

                PassStructAsJLongPointerToStructCopy passStruct
                    => $"FumoCement::toJavaPointer(new {passStruct.Record.Name}({passStruct.ValueToPass}))",

                PassEnumValueAsValueTypeToEnum passEnum
                    => $"static_cast<long>({passEnum.ValueToPass})",

                PassCallbackObject passCallbackObject
                    => $"{passCallbackObject.ContextVariable}->globalObjectRef",

                _ => throw UnsupportedType(valuePass)
            };
        }

        private string PassToNative(ValuePass valuePass, string envVariable = "env")
        {
            if (valuePass is PassFunctionPointerProxyLambda passProxy)
            {
                WriteFunctionPointerLambda(passProxy);
                return ""; // TODO: remove this dirty hack
            }

            return valuePass switch {
                PassPrimitive passPrimitive
                    => $"FumoCement::toNativePrimitive({passPrimitive.ValueToPass})",

                PassPointerAsJLongToPtr passPointer
                    => $"FumoCement::toNativePointer<{passPointer.Pointer.PointeeType.AsRawString}>({passPointer.ValueToPass})",

                PassStringAsJByteArrayToCharPtr { RequiresDeletionEnum: false } passString
                    => $"FumoCement::toCppString({envVariable}, {passString.ValueToPass})",

                PassNestedStructAsJLongPointerToStructPtr passNestedStruct
                    => $"FumoCement::toNativePointer<{passNestedStruct.Record.Name}>({passNestedStruct.ValueToPass})",

                PassStructAsJLongPointerToStructCopy passStruct
                    => $"*FumoCement::toNativePointer<{passStruct.Record.Name}>({passStruct.ValueToPass})",

                PassEnumValueAsValueTypeToEnum passEnum
                    => $"static_cast<{passEnum.EnumType.Name}>({passEnum.ValueToPass})",

                PassContextPtrAsVoidPtr passContextPtr
                    => $"reinterpret_cast<void*>({passContextPtr.JavaFunctionHandleVariable})",

                _ => throw UnsupportedType(valuePass)
            };
        }

        // Base constructs

        private void WriteTemplateStringField(string name, string value)
        {
            WriteIndentedLine($"using {name} = FumoCement::TemplateString<");
            for (var i = 0; i < value.Length; i++)
            {
                var character = value[i];
                Write('\'');
                Write(character == '\'' ? @"\'" : character.ToString());
                Write('\'');
                if (i != value.Length - 1)
                {
                    Write(", ");
                }
            }

            Write(">;");
        }

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

        private void BeginNativeLambda(NativeMethod nativeLambda)
        {
            Write("[]");
            Write("(");
            for (var i = 0; i < nativeLambda.Parameters.Count; i++)
            {
                var param = nativeLambda.Parameters[i];
                Write(param.Type.AsRawString);
                Write(" ");
                Write(param.Name);
                if (i != nativeLambda.Parameters.Count - 1)
                {
                    Write(", ");
                }
            }

            Write(") -> ");
            Write(nativeLambda.ReturnType.AsRawString);
            WriteBlockStart();
        }

        private void EndNativeLambda() => WriteBlockEnd();

        private static string MangleNameForJniMethod(string name)
        {
            var builder = new StringBuilder(name.Length + 16);
            foreach (var character in name)
            {
                if (character is
                        (>= 'a' and <= 'z') or
                        (>= 'A' and <= 'Z') ||
                    char.IsDigit(character))
                {
                    _ = builder.Append(character);
                }
                else
                {
                    _ = builder.Append(character switch {
                        '_' => "_1",
                        ';' => "_2",
                        '[' => "_3",
                        _ => $"_0{(int)character:X4}"
                    });
                }
            }

            return builder.ToString();
        }
    }
}
