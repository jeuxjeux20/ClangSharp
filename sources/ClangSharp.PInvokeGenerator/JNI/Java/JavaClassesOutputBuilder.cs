// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ClangSharp.JNI.Generation.Enum;
using ClangSharp.JNI.Generation.FunctionPointer;
using ClangSharp.JNI.Generation.Method;
using ClangSharp.JNI.Generation.Struct;
using ClangSharp.JNI.Generation.Transitions;
using Microsoft.VisualBasic.FileIO;

namespace ClangSharp.JNI.Java
{
    internal class JavaClassesOutputBuilder : JniOutputBuilderBase
    {
        public JavaClassesOutputBuilder(string name, PInvokeGeneratorConfiguration configuration,
            string indentationString = DefaultIndentationString) : base(name, configuration,
            indentationString)
        {
            CurrentBraceStyle = BraceStyle.KAndR;
        }

        public override string Extension => ".java";
        public override bool IsTestOutput => false;

        protected override void WriteContent()
        {
            foreach (var @struct in GenerationContext.GetTransformationUnits<StructTransformationUnit>())
            {
                GenerateStruct(@struct);
            }

            foreach (var @enum in GenerationContext.GetTransformationUnits<EnumTransformationUnit>())
            {
                GenerateEnumClass(@enum.ClassGenerationUnit);
            }

            foreach (var funcPointer in GenerationContext.GetTransformationUnits<FunctionPointerTransformationUnit>())
            {
                GenerateCallbackInterface(funcPointer);
            }

            foreach (var methodGen in GenerationContext.GetTransformationUnits<MethodTransformationUnit>())
            {
                GenerateDownstreamMethod(methodGen.MethodGenerationUnit);
            }
        }

        private void GenerateStruct(StructTransformationUnit structTransformationUnit)
        {
            var @struct = structTransformationUnit.ClassGenerationUnit;
            var className = @struct.JavaName;

            WriteIndentedLine($"public static final class {className} extends NativeStruct");
            WriteBlockStart();

            WriteIndentedLine($"public static native long {StructClassGenerationUnit.AllocateStructMethodName}();");
            WriteIndentedLine(
                $"public static native void {StructClassGenerationUnit.DestroyStructMethodName}(long handle);");
            WriteIndentedLine($"public static native void {StructClassGenerationUnit.OverwriteMethodName}(" +
                              $"@Pointer(\"{@struct.NativeName}*\") long targetHandle, " +
                              $"@Pointer(\"{@struct.NativeName}*\") long dataHandle);");

            WriteNewLine();
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

            foreach (var fieldAccessorGen in structTransformationUnit.FieldAccessorGenerationUnits)
            {
                GenerateDownstreamMethod(fieldAccessorGen);
            }

            WriteBlockEnd();
        }

        private void GenerateEnumClass(EnumClassGenerationUnit @enum)
        {
            WriteIndentedLine($"public final class {@enum.JavaName}");
            WriteBlockStart();

            foreach (var enumField in @enum.Fields)
            {
                WriteIndentedLine($"public static final {enumField.Type} {enumField.Name} = {enumField.Value};");
            }

            WriteBlockEnd();
        }

        private void GenerateCallbackInterface(FunctionPointerTransformationUnit transformationUnit)
        {
            var interfaceGen = transformationUnit.InterfaceGenerationUnit;

            WriteIndentedLine($"public interface {interfaceGen.Name}");
            WriteBlockStart();

            GenerateUpstreamMethod(transformationUnit.MethodGenerationUnit);

            WriteBlockEnd();
        }

        private void GenerateDownstreamMethod(DownstreamMethodGenerationUnit methodGen)
        {
            var nativeMethod = methodGen.JavaNativeMethod;
            var publicMethod = methodGen.JavaMethod;

            WriteBodylessMethod(nativeMethod, "public");

            BeginFullJavaMethod(publicMethod);

            var finalMethod = methodGen.JavaNativeMethod.Name;
            JavaTransitionWriter.Write(this, methodGen, TransitionKind.JavaToJni,
                parameters => new StringBuilder()
                    .AppendMethodCallExpression(finalMethod, parameters, x => x.IntermediateName)
                    .ToString());

            EndFullJavaMethod();
        }

        private void GenerateUpstreamMethod(UpstreamMethodGenerationUnit methodGen)
        {
            var callbackMethod = methodGen.CallbackMethod;
            var callbackCallerMethod = methodGen.CallbackCallerMethod;

            WriteBodylessMethod(callbackMethod, "public");

            BeginFullJavaMethod(callbackCallerMethod);

            string MakeCallbackCall(ImmutableArray<TransitingMethodParameter> parameters)
            {
                var callbackObject = methodGen.CallbackObjectParameter.IntermediateName;
                var finalMethod = $"{callbackObject}.{callbackMethod.Name}";

                var parametersExceptCallback = parameters.Remove(methodGen.CallbackObjectParameter);
                return new StringBuilder()
                    .AppendMethodCallExpression(finalMethod, parametersExceptCallback, p => p.IntermediateName)
                    .ToString();
            }

            JavaTransitionWriter.Write(this, methodGen, TransitionKind.JniToJava, MakeCallbackCall);

            EndFullJavaMethod();
        }

        // TODO: Move visibility to method
        private void WriteBodylessMethod(BodylessJavaMethod method, string visibility)
        {
            WriteIndentedLine();
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
            RawBuilder.AppendMethodParameters(method.Parameters, x => x.ToString());
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
            RawBuilder.AppendMethodParameters(method.Parameters, x => x.ToString());
            WriteBlockStart();
        }

        private void EndFullJavaMethod()
        {
            WriteBlockEnd();
        }
    }
}
