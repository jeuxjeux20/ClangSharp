// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualBasic.FileIO;

namespace ClangSharp.JNI.Java
{
    internal class JavaClassesOutputBuilderOld : JniOutputBuilderBaseOld
    {
        public JavaClassesOutputBuilderOld(string name, PInvokeGeneratorConfiguration configuration,
        string indentationString = DefaultIndentationString) : base(name, configuration,
            indentationString)
        {
            CurrentBraceStyle = BraceStyle.KAndR;
        }

        public override string Extension => ".java";
        public override bool IsTestOutput => false;

        protected override void WriteContent()
        {
            foreach (var @struct in CurrentGenerationPlan.Structs)
            {
                GenerateStruct(@struct);
            }

            foreach (var @enum in CurrentGenerationPlan.Enums)
            {
                GenerateEnum(@enum);
            }

            foreach (var callback in CurrentGenerationPlan.JavaCallbacks)
            {
                GenerateCallback(callback);
            }

            foreach (var method in CurrentGenerationPlan.Methods)
            {
                GenerateMethod(method);
            }
        }

        private void GenerateStruct(StructGenerationInfo @struct)
        {
            var className = @struct.JavaName;

            WriteIndentedLine($"public static final class {className} extends NativeStruct");
            WriteBlockStart();

            WriteIndentedLine($"public static native long {@struct.AllocateStructMethodName}();");
            WriteIndentedLine($"public static native void {@struct.DestroyStructMethodName}(long handle);");
            WriteNewLine();
            WriteIndentedLine($"public static native void {@struct.OverwriteMethodName}(" +
                              $"@Pointer(\"{@struct.NativeName}*\") long targetHandle, " +
                              $"@Pointer(\"{@struct.NativeName}*\") long dataHandle);");

            WriteIndentedLine($"private static final NativeObjectTracker<{className}> ownedTracker = " +
                              $"new NativeObjectTracker<>({className}::new, NativeObjectTracker.Target.OWNED_OBJECTS);");
            WriteIndentedLine($"private static final NativeObjectTracker<{className}> unownedTracker = " +
                              $"new NativeObjectTracker<>({className}::new, NativeObjectTracker.Target.UNOWNED_OBJECTS);");
            WriteNewLine();

            WriteIndentedLine($"public static {className} createTracked()");
            WriteBlockStart();
            WriteIndentedLine("return ownedTracker.getOrCreate(allocateStruct());");
            WriteBlockEnd();
            WriteNewLine();

            WriteIndentedLine($"public static {className} getTrackedAndOwned(long handle)");
            WriteBlockStart();
            WriteIndentedLine("return ownedTracker.getOrCreate(handle);");
            WriteBlockEnd();
            WriteNewLine();

            WriteIndentedLine($"public static {className} getTrackedAndUnowned(long handle)");
            WriteBlockStart();
            WriteIndentedLine("return unownedTracker.getOrCreate(handle);");
            WriteBlockEnd();
            WriteNewLine();

            WriteIndentedLine($"public {className}()");
            WriteBlockStart();
            WriteIndentedLine($"super(allocateStruct(), true, {className}::destroyStruct);");
            WriteBlockEnd();
            WriteNewLine();

            WriteIndentedLine($"public {className}(long handle, boolean isOwned)");
            WriteBlockStart();
            WriteIndentedLine($"super(handle, isOwned, {className}::destroyStruct);");
            WriteBlockEnd();

            WriteIndentedLine($"public {className}(long handle, boolean isOwned, DisposalMethod disposalMethod)");
            WriteBlockStart();
            WriteIndentedLine($"super(handle, isOwned, disposalMethod, {className}::destroyStruct);");
            WriteBlockEnd();
            WriteNewLine();

            WriteIndentedLine($"public void overwrite(@Pointer(\"{@struct.NativeName}*\") long dataHandle)");
            WriteBlockStart();
            WriteIndentedLine("overwrite(getHandle(), dataHandle);");
            WriteBlockEnd();

            foreach (var field in @struct.Fields)
            {
                WriteNewLine();

                WriteStructFieldGetter(field);
                WriteStructFieldSetter(field);
            }

            WriteBlockEnd();
        }

        private void GenerateEnum(EnumGenerationInfo @enum)
        {
            WriteIndentedLine($"public final class {@enum.JavaName}");
            WriteBlockStart();

            foreach (var enumConstant in @enum.Constants)
            {
                var type = enumConstant.IsSigned ? "int" : "@Unsigned int";
                var value = enumConstant.SignedValue ??
                            (long) (enumConstant.UnsignedValue ??
                                    throw new InvalidOperationException("Both values are null."));

                WriteIndentedLine($"public static final {type} {enumConstant.JavaName} = {value};");
            }

            WriteBlockEnd();
        }

        private void GenerateCallback(JavaCallbackGenerationInfo callback)
        {
            var generationSet = callback.CallbackGenerationSet;

            WriteIndentedLine($"public interface {callback.CallbackInterface}");
            WriteBlockStart();

            WriteBodylessMethod(generationSet.UpstreamInterfaceMethod, null);
            WriteNewLine();

            BeginFullJavaMethod(generationSet.CallbackCallMethod);

            var parameterPasses = generationSet.UpstreamParameterPasses;
            var callbackCallMethod = generationSet.CallbackCallMethod;
            var interfaceMethod = generationSet.UpstreamInterfaceMethod;
            var callbackObject = generationSet.CallbackCallMethod.Parameters[0].Name;

            for (var i = 0; i < parameterPasses.Count; i++)
            {
                var parameterPass = parameterPasses[i];
                var parameterType = interfaceMethod.Parameters[i].Type;

                WriteIndentedLine($"{parameterType.AsString} {parameterPass.IntermediateVariableName} = ");
                Write(PassToJava(parameterPass));
                Write(';');
            }

            var returnPass = generationSet.UpstreamReturnValuePass;
            var hasReturnValue = returnPass != null;

            WriteNewLine();
            WriteIndentation();
            if (hasReturnValue)
            {
                Write($"{callbackCallMethod.ReturnType} {returnPass.ValueToPass} = ");
            }

            // Call the native method.
            Write(callbackObject);
            Write(".");
            Write(interfaceMethod.Name);
            WriteMethodArgumentsFromIntermediate(parameterPasses);
            Write(";");

            if (hasReturnValue)
            {
                WriteIndentedLine("return ");
                Write(PassToJni(returnPass));
                Write(";");
            }

            EndFullJavaMethod();

            WriteBlockEnd();
        }

        private void GenerateMethod(MethodGenerationInfo method)
        {
            var generationSet = method.GenerationSet;

            var nativeMethod = generationSet.InternalJavaNativeMethod;
            var publicMethod = generationSet.PublicJavaMethod;
            var parameterPasses = generationSet.JavaToJniParameterPasses;

            WriteBodylessMethod(nativeMethod, "public");

            BeginFullJavaMethod(publicMethod);

            for (var i = 0; i < parameterPasses.Count; i++)
            {
                var parameterPass = parameterPasses[i];
                var parameterType = nativeMethod.Parameters[i].Type;

                WriteIndentedLine($"{parameterType.AsString} {parameterPass.IntermediateVariableName} = ");
                Write(PassToJni(parameterPass));
                Write(';');
            }

            var returnPass = generationSet.JavaToJniReturnValuePass;
            var hasReturnValue = returnPass != null;

            WriteNewLine();
            WriteIndentation();
            if (hasReturnValue)
            {
                Write($"{nativeMethod.ReturnType.AsString} {returnPass.ValueToPass} = ");
            }

            // Call the native method.
            Write(nativeMethod.Name);
            WriteMethodArgumentsFromIntermediate(parameterPasses);
            Write(";");

            if (hasReturnValue)
            {
                WriteIndentedLine("return ");
                Write(PassToJava(returnPass));
                Write(";");
            }

            EndFullJavaMethod();
        }

        private string PassToJava(ValuePass pass)
        {
            return pass switch {
                PassPrimitive or PassPointerAsJLongToPtr or PassEnumValueAsValueTypeToEnum
                    => ((StandaloneValuePass)pass).ValueToPass,

                PassNestedStructAsJLongPointerToStructPtr passStruct
                    => $"{passStruct.JavaStructName}.getTrackedAndUnowned({passStruct.ValueToPass})",

                PassStructAsJLongPointerToStructCopy passStruct
                    => $"{passStruct.JavaStructName}.getTrackedAndOwned({passStruct.ValueToPass})",

                PassStringAsJByteArrayToCharPtr passString
                    => $"{passString.ValueToPass} == null ? null : new String({passString.ValueToPass})",

                PassStringDeletionEnumAsBool
                    => throw UnsupportedPass("Cannot pass a StringDeletionEnum to Java.", pass),

                _ => throw UnsupportedPass(pass)
            };
        }

        private string PassToJni(ValuePass pass)
        {
            return pass switch {
                PassPrimitive or
                    PassPointerAsJLongToPtr or
                    PassEnumValueAsValueTypeToEnum
                    => ((StandaloneValuePass)pass).ValueToPass,

                PassNestedStructAsJLongPointerToStructPtr or
                    PassStructAsJLongPointerToStructCopy or
                    PassFunctionPointerAsContextPtr
                    => $"{((StandaloneValuePass)pass).ValueToPass}.getHandle()",

                PassStringAsJByteArrayToCharPtr passString
                    => $"{passString.ValueToPass}.getBytes()",

                PassStringDeletionEnumAsBool passStringDeletionEnum
                    => $"{passStringDeletionEnum.ValueToPass}.isDeletingString()",

                PassStructHandleAsJLong
                    => "getHandle()",

                _ => throw UnsupportedPass(pass)
            };
        }

        private void WriteStructFieldGetter(StructFieldGenerationInfo structField)
        {
            var generationSet = structField.GetterGenerationSet;

            var nativeMethod = generationSet.InternalJavaNativeMethod;
            var publicMethod = generationSet.PublicJavaMethod;
            var parameterPasses = generationSet.JavaToJniParameterPasses;

            WriteBodylessMethod(nativeMethod, "public");

            BeginFullJavaMethod(publicMethod);

            for (var i = 0; i < parameterPasses.Count; i++)
            {
                var parameterPass = parameterPasses[i];
                var parameterType = nativeMethod.Parameters[i].Type;

                WriteIndentedLine($"{parameterType} {parameterPass.IntermediateVariableName} = " +
                                  $"{PassToJni(parameterPass)};");
            }

            var returnPass = generationSet.JavaToJniReturnValuePass ??
                             throw new InvalidOperationException("No valid return value pass in struct getter.");

            WriteIndentedLine($"{nativeMethod.ReturnType} {returnPass.ValueToPass} = ");
            Write(nativeMethod.Name);
            WriteMethodArgumentsFromIntermediate(parameterPasses);
            Write(";");

            WriteIndentedLine($"return {PassToJava(returnPass)};");

            EndFullJavaMethod();
        }

        private void WriteStructFieldSetter(StructFieldGenerationInfo structField)
        {
            var generationSet = structField.SetterGenerationSet;

            var nativeMethod = generationSet.InternalJavaNativeMethod;
            var publicMethod = generationSet.PublicJavaMethod;
            var parameterPasses = generationSet.JavaToJniParameterPasses;

            WriteNewLine();

            WriteBodylessMethod(nativeMethod, "public");

            BeginFullJavaMethod(publicMethod);

            for (var i = 0; i < generationSet.JavaToJniParameterPasses.Count; i++)
            {
                var parameterPass = generationSet.JavaToJniParameterPasses[i];
                var parameterType = nativeMethod.Parameters[i].Type;

                WriteIndentedLine($"{parameterType} {parameterPass.IntermediateVariableName} = " +
                                  $"{PassToJni(parameterPass)};");
            }

            WriteIndentedLine($"{nativeMethod.Name}");
            WriteMethodArgumentsFromIntermediate(parameterPasses);
            Write(";");

            EndFullJavaMethod();
        }

        /*CODE TO RESTORE
        private void WriteStructFieldOffsetConstant(in StructFieldGenInfo structField, out string fieldName)
        {
            fieldName = null;

            // TODO: Implement something in C++ to make this work.
            var offset = -1;
            if (offset == -1)
            {
                return;
            }

            fieldName = JavaConventions.ToScreamingCase(structField.EscapedName) + "_OFFSET";
            WriteIndentedLine($"private static final int {fieldName} = {offset};");
        }

        private void WriteStructFieldValueObjectCache(in StructFieldGenInfo structField, string annotatedActualType, out string fieldName)
        {
            fieldName = structField.EscapedName + "ValueObjectCache";
            WriteIndentedLine($"private {annotatedActualType} {fieldName};");
        }
        */

        // TODO: Move visibility to method
        private void WriteBodylessMethod(BodylessJavaMethod method, string visibility)
        {
            WriteIndentedLine("");
            if (!string.IsNullOrEmpty(visibility))
            {
                Write(visibility);
                Write(' ');
            }

            if (method.IsStatic)
            {
                Write("static ");
            }

            if (method.IsNative)
            {
                Write("native ");
            }

            Write(method.ReturnType);
            Write(" ");
            Write(method.Name);
            WriteMethodParameters(method.Parameters);
            Write(";");
        }

        private void BeginFullJavaMethod(FullJavaMethod method)
        {
            WriteIndentedLine("public ");
            if (method.IsStatic)
            {
                Write("static ");
            }

            Write(method.ReturnType);
            Write(" ");
            Write(method.Name);
            WriteMethodParameters(method.Parameters);
            WriteBlockStart();
        }

        private void EndFullJavaMethod()
        {
            WriteBlockEnd();
        }

        private void WriteMethodArguments(IReadOnlyList<string> arguments)
        {
            Write("(");
            for (var i = 0; i < arguments.Count; i++)
            {
                var argument = arguments[i];
                Write(argument);
                if (i != arguments.Count - 1)
                {
                    Write(", ");
                }
            }

            Write(")");
        }

        private void WriteMethodArgumentsFromIntermediate(IReadOnlyList<ValuePass> parameterPasses)
        {
            WriteMethodArguments(parameterPasses.Select(x => x.IntermediateVariableName).ToArray());
        }

        private void WriteMethodParameters(IReadOnlyList<MethodParameter<JavaType>> parameters)
        {
            Write("(");
            for (var i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                Write(parameter.Type.AsString);
                Write(" ");
                Write(parameter.Name);

                if (i != parameters.Count - 1)
                {
                    Write(", ");
                }
            }

            Write(")");
        }
    }
}
