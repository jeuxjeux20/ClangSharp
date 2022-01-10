// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClangSharp.JNI.Java;

namespace ClangSharp.JNI.JNIGlue
{
    internal sealed class JniGlueOutputBuilder : JniOutputBuilderBase
    {
        public JniGlueOutputBuilder(string name, string @namespace,
            string container,
            string indentationString = DefaultIndentationString) : base(name,
            indentationString, @namespace, container)
        {
        }

        public override bool IsTestOutput => false;
        public override string Extension => ".h";

        protected override void WriteContent()
        {
            foreach (var @struct in CurrentGenerationPlan.Structs)
            {
                WriteIndentedLine($"// Struct declaration: {@struct.NativeName}");

                WriteAllocateStructMethod(@struct);
                WriteDestroyStructMethod(@struct);
                WriteOverwriteMethod(@struct);

                foreach (var structField in @struct.Fields)
                {
                    WriteStructFieldGetter(@struct, structField);
                    WriteStructFieldSetter(@struct, structField);
                }

                WriteIndentedLine($"// End struct declaration: {@struct.NativeName}");
            }

            foreach (var method in CurrentGenerationPlan.Methods)
            {
                WriteMethod(method);
            }
        }

        private void WriteAllocateStructMethod(StructGenerationInfo @struct)
        {
            BeginJniMethod(JniType.JLong,
                @struct.JavaType.RawName,
                @struct.AllocateStructMethodName,
                true,
                Array.Empty<MethodParameter<JniType>>());

            WriteIndentedLine($"return FumoCement::toJavaPointer(new {@struct.NativeName});");

            EndJniMethod();
        }

        private void WriteDestroyStructMethod(StructGenerationInfo @struct)
        {
            BeginJniMethod(JniType.Void,
                @struct.JavaType.RawName,
                @struct.DestroyStructMethodName,
                true,
                new MethodParameter<JniType>[] { new(JniType.JLong, "handle") });

            WriteIndentedLine($"delete FumoCement::toNativePointer<{@struct.NativeName}>(handle);");

            EndJniMethod();
        }

        private void WriteOverwriteMethod(StructGenerationInfo @struct)
        {
            BeginJniMethod(JniType.Void,
                @struct.JavaType.RawName,
                @struct.OverwriteMethodName,
                true,
                new MethodParameter<JniType>[] {
                    new(JniType.JLong, "targetHandle"),
                    new(JniType.JLong, "dataHandle")
                });

            WriteIndentedLine($"*FumoCement::toNativePointer<{@struct.NativeName}>(targetHandle) = " +
                              $"*FumoCement::toNativePointer<{@struct.NativeName}>(dataHandle);");

            EndJniMethod();
        }

        private void WriteStructFieldGetter(StructGenerationInfo @struct, StructFieldGenerationInfo structField)
        {
            var generationSet = structField.GetterGenerationSet;
            var glueMethod = generationSet.JniGlueMethod;

            BeginJniMethod(glueMethod);

            if (generationSet.JniToCReturnValuePass is not { } returnValuePass)
            {
                throw new InvalidOperationException("Return value pass is void on struct field getter.");
            }

            var handleName = glueMethod.Parameters[0].Name;

            WriteIndentedLine(
                $"auto& {returnValuePass.ValueToPass} = " +
                $"(FumoCement::toNativePointer<{@struct.NativeName}>({handleName}))->{structField.Name};");

            WriteIndentedLine($"return {PassToJava(returnValuePass)};");

            EndJniMethod();
        }

        private void WriteStructFieldSetter(StructGenerationInfo @struct, StructFieldGenerationInfo structField)
        {
            var generationSet = structField.SetterGenerationSet;
            var glueMethod = generationSet.JniGlueMethod;

            BeginJniMethod(glueMethod);

            // TODO: Dirty, maybe create some StructFieldSetterMethodGenerationSet
            var handleName = glueMethod.Parameters[0].Name;
            var setterValuePass = (StandaloneValuePass)generationSet.JniToCParameterPasses[0];

            WriteIndentedLine(
                $"auto& {setterValuePass.IntermediateVariableName} = " +
                $"(FumoCement::toNativePointer<{@struct.NativeName}>({handleName}))->{structField.Name};");

            WriteIndentedLine($"{setterValuePass.IntermediateVariableName} = FumoCement::passAsC(" +
                              $"{PassToNative(setterValuePass)}" +
                              ");");

            EndJniMethod();
        }

        private void WriteMethod(MethodGenerationInfo method)
        {
            var generationSet = method.GenerationSet;

            var glueMethod = generationSet.JniGlueMethod;
            var returnPass = generationSet.JniToCReturnValuePass;
            var parameterPasses = generationSet.JniToCParameterPasses;

            BeginJniMethod(glueMethod);

            // Put all the parameters in intermediate variables.
            PassParametersInIntermediateVariables(parameterPasses, PassToNative);

            // Call the method (and put the return value in a variable if any)
            WriteIndentedLine();
            if (returnPass is not null)
            {
                Write($"auto&& {returnPass.ValueToPass} = ");
            }

            Write(method.NativeMethod.Name);
            Write('(');
            PassIntermediateVariablesAsArgs(parameterPasses);
            Write(");");

            if (returnPass is not null)
            {
                WriteIndentedLine("return ");
                Write(PassToJava(returnPass));
                Write(";");
            }

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
                method.ContainingType,
                method.Name,
                true,
                method.Parameters);
        }

        private void BeginJniMethod(string returnType, string javaType, string javaMethodName, bool isStatic,
            IEnumerable<MethodParameter<JniType>> arguments)
        {
            WriteIndentedLine("JNIEXPORT ");
            Write(returnType);
            Write(" JNICALL ");

            Write("Java_");
            Write(JavaConventions.CPackageName(Namespace));
            Write("_");
            Write(MangleNameForJniMethod(javaType));
            Write("_");
            Write(MangleNameForJniMethod(javaMethodName));

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
