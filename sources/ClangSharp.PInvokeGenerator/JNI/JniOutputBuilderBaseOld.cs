// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using ClangSharp.Abstractions;
using ClangSharp.CSharp;
using ClangSharp.JNI.Java;

namespace ClangSharp.JNI
{
    internal abstract class JniOutputBuilderBaseOld : IOutputBuilder
    {
        public const string DefaultIndentationString = "    ";

        protected JniGenerationPlan CurrentGenerationPlan { get; }

        protected BraceStyle CurrentBraceStyle { get; set; } = BraceStyle.Allman;
        protected StringBuilder ContentStringBuilder { get; } = new();
        private int _indentationLevel = new();
        private readonly PInvokeGeneratorConfiguration _configuration;
        private readonly string _indentationString;

        private PreliminaryEnum _preliminaryEnum;
        private PreliminaryFunction _preliminaryFunction;
        private PreliminaryStruct _preliminaryStruct;

        public bool IsUncheckedContext => true;

        protected JniOutputBuilderBaseOld(string name, PInvokeGeneratorConfiguration configuration,
            string indentationString)
        {
            Name = name;
            _configuration = configuration;
            _indentationString = indentationString;
            Namespace = configuration.DefaultNamespace;
            CurrentGenerationPlan = new JniGenerationPlan { ContainerClass = configuration.MethodClassName, Package = Namespace };
        }

        protected static UnsupportedJniScenarioException UnsupportedType<T>(T type)
            => new($"Type not supported: {type}.");

        protected static UnsupportedJniScenarioException UnsupportedPass(string message, ValuePass valuePass)
            => new($"Cannot handle {valuePass}: {message}");

        protected static UnsupportedJniScenarioException UnsupportedPass(ValuePass valuePass)
            => new($"Cannot handle {valuePass} in this scenario.");

        public abstract string Extension { get; }
        public abstract bool IsTestOutput { get; }
        public string Name { get; }

        public string Namespace { get; }

        public string Content
        {
            get
            {
                WriteContent();
                var content = ContentStringBuilder.ToString();
                _ = ContentStringBuilder.Clear();
                return content;
            }
        }

        protected abstract void WriteContent();

        public void WriteIndentation()
        {
            for (var i = 0; i < _indentationLevel; i++)
            {
                _ = ContentStringBuilder.Append(_indentationString);
            }
        }

        public void Write<T>(T value)
        {
            _ = ContentStringBuilder.Append(value);
        }

        public void WriteIndentedLine(string value = "")
        {
            _ = ContentStringBuilder.AppendLine();
            WriteIndentation();
            _ = ContentStringBuilder.Append(value);
        }

        public void WriteNewLine()
        {
            _ = ContentStringBuilder.AppendLine();
        }

        public void DecreaseIndentation()
        {
            if (_indentationLevel == 0)
            {
                throw new InvalidOperationException();
            }

            _indentationLevel--;
        }

        public void IncreaseIndentation() => _indentationLevel++;

        public void WriteBlockStart()
        {
            switch (CurrentBraceStyle)
            {
                case BraceStyle.Allman:
                {
                    WriteIndentedLine("{");
                    break;
                }
                case BraceStyle.KAndR:
                {
                    Write(" {");
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            IncreaseIndentation();
        }

        public void WriteBlockEnd()
        {
            DecreaseIndentation();
            WriteIndentedLine("}");
        }

        public virtual void BeginInnerValue()
        {
            // nop, send help ;_;
        }

        public virtual void EndInnerValue()
        {
            // nop, send help ;_;
        }

        public virtual void BeginInnerCast()
        {
            // nop, send help ;_;
        }

        public virtual void WriteCastType(string targetTypeName)
        {
            // nop, send help ;_;
        }

        public virtual void EndInnerCast()
        {
            // nop, send help ;_;
        }

        public virtual void BeginUnchecked()
        {
            // nop, send help ;_;
        }

        public virtual void EndUnchecked()
        {
            // nop, send help ;_;
        }

        public void BeginValue(in ValueDesc desc)
        {
            if (desc.IsConstant && _preliminaryEnum != null)
            {
                _preliminaryEnum.PreliminaryConstant =
                    new PreliminaryEnumConstant(JavaConventions.EscapeName(desc.EscapedName));
            }
        }

        public virtual void WriteConstantValue(long value)
        {
            _preliminaryEnum?.CommitSignedPreliminaryConstant(value);
        }

        public virtual void WriteConstantValue(ulong value)
        {
            _preliminaryEnum?.CommitUnsignedPreliminaryConstant(value);
        }

        public void EndValue(in ValueDesc desc)
        {
            // nop!
        }

        public virtual void EndConstantValue()
        {
            // nop, send help ;_;
        }

        public virtual void EndConstant(bool isConstant)
        {
            // nop, send help ;_;
        }

        public virtual void BeginEnum(in EnumDesc desc)
        {
            _preliminaryEnum = new PreliminaryEnum { JavaName = JavaConventions.EscapeName(desc.EscapedName) };
        }

        public virtual void EndEnum(in EnumDesc desc)
        {
            CurrentGenerationPlan.Enums.Add(
                new EnumGenerationInfo(_preliminaryEnum.JavaName, _preliminaryEnum.Constants));

            _preliminaryEnum = null;
        }

        public virtual void BeginField(in FieldDesc desc)
        {
            if (_preliminaryStruct is null)
            {
                return;
            }

            var canonicalType = desc.NativeCanonicalType;

            if (canonicalType == null)
            {
                Console.WriteLine("Unsupported field type: " + desc.NativeTypeName + $" ({desc.EscapedName})");
                return;
            }

            var field = new PreliminaryStructField(JavaConventions.EscapeName(desc.EscapedName),
                canonicalType);

            MethodGenerationSet getterGen;
            try
            {
                getterGen = CreateFieldGetterMethodGenerationSet(field);
            }
            catch (UnsupportedJniScenarioException e)
            {
                Console.WriteLine($"Failed to create a getter for struct {_preliminaryStruct.JavaName}: {e.Message}");
                return;
            }

            MethodGenerationSet setterGen;
            try
            {
                setterGen = CreateFieldSetterMethodGenerationSet(field);
            }
            catch (UnsupportedJniScenarioException e)
            {
                Console.WriteLine($"Failed to create a setter for struct {_preliminaryStruct.JavaName}: {e.Message}");
                return;
            }

            _preliminaryStruct.Fields.Add(new StructFieldGenerationInfo(
                field.Name,
                field.Type,
                getterGen,
                setterGen
            ));
        }

        public virtual void WriteFixedCountField(string typeName, string escapedName, string fixedName, string count)
        {
            // nop, send help ;_;
        }

        public virtual void WriteRegularField(string typeName, string escapedName)
        {
        }

        public void EndField(in FieldDesc desc)
        {

        }

        public void BeginFunctionInnerPrototype(in FunctionOrDelegateDesc info)
        {

        }

        public void BeginParameter(in ParameterDesc info)
        {
            if (_preliminaryFunction is null)
            {
                return;
            }

            var canonicalType = info.CanonicalNativeType;
            if (canonicalType is null)
            {
                Console.WriteLine($"Unsupported method parameter type in {_preliminaryFunction.JavaName}: {info.Type}");
                _preliminaryFunction = null;
                return;
            }

            _preliminaryFunction.Parameters.Add(new MethodParameter<TypeDesc>(
                canonicalType,
                JavaConventions.EscapeName(info.Name)
            ));
        }

        public virtual void BeginFunctionOrDelegate(in FunctionOrDelegateDesc info, ref bool isMethodClassUnsafe)
        {
            if (info.CanonicalNativeType is not FunctionProtoTypeDesc canonicalType)
            {
                Console.WriteLine("Unsupported function type: " + info.NativeTypeName + $" ({info.EscapedName})");
                return;
            }

            _preliminaryFunction = new PreliminaryFunction {
                NativeName = info.EscapedName,
                JavaName = JavaConventions.EscapeName(info.EscapedName), // TODO: remove escape
                ReturnType = canonicalType.ReturnType
            };
        }

        public virtual void EndParameter(in ParameterDesc info)
        {

        }

        public virtual void BeginParameterDefault()
        {
            // nop, send help ;_;
        }

        public virtual void EndParameterDefault()
        {
            // nop, send help ;_;
        }

        public virtual void WriteParameterSeparator()
        {
            // nop, send help ;_;
        }

        public void EndFunctionInnerPrototype(in FunctionOrDelegateDesc info)
        {
            // nop!
        }

        public virtual void BeginConstructorInitializer(string memberRefName, string memberInitName)
        {
            // nop, send help ;_;
        }

        public virtual void EndConstructorInitializer()
        {
            // nop, send help ;_;
        }

        public virtual void BeginBody(bool isExpressionBody = false)
        {
            // nop, send help ;_;
        }

        public virtual void BeginConstructorInitializers()
        {
            // nop, send help ;_;
        }

        public virtual void EndConstructorInitializers()
        {
            // nop, send help ;_;
        }

        public virtual void BeginInnerFunctionBody()
        {
            // nop, send help ;_;
        }

        public virtual void EndInnerFunctionBody()
        {
            // nop, send help ;_;
        }

        public virtual void EndBody(bool isExpressionBody = false)
        {
            // nop, send help ;_;
        }

        public void EndFunctionOrDelegate(in FunctionOrDelegateDesc info)
        {
            if (_preliminaryFunction is null)
            {
                return;
            }

            var nativeMethod = new NativeMethod(_preliminaryFunction.NativeName,
                _preliminaryFunction.ReturnType, _preliminaryFunction.Parameters.ToImmutableArray());

            MethodGenerationSet generationSet;
            try
            {
                generationSet = CreateFunctionMethodGenerationSet(_preliminaryFunction);
            }
            catch (UnsupportedJniScenarioException e)
            {
                Console.WriteLine($"Failed to write method {_preliminaryFunction.NativeName}: {e.Message}");
                throw;
            }

            CurrentGenerationPlan.Methods.Add(new MethodGenerationInfo(nativeMethod, generationSet));

            _preliminaryFunction = null;
        }

        public virtual void BeginStruct(in StructDesc info)
        {
            if (info.IsComplete)
            {
                var javaName = JavaConventions.EscapeName(info.EscapedName);

                _preliminaryStruct =
                    new PreliminaryStruct(javaName, CurrentGenerationPlan.StructTypeInContainer(javaName));
            }
        }

        public void BeginMarkerInterface(string[] baseTypeNames)
        {
            // nop!
        }

        public void EndMarkerInterface()
        {
            // nop!
        }

        public virtual void BeginExplicitVtbl()
        {
            // nop, send help ;_;
        }

        public virtual void EndExplicitVtbl()
        {
            // nop, send help ;_;
        }

        public void EndStruct(in StructDesc info)
        {
            if (_preliminaryStruct is null)
            {
                return;
            }

            CurrentGenerationPlan.Structs.Add(
                CurrentGenerationPlan.MakeStructGenerationInfo(_preliminaryStruct.JavaName, _preliminaryStruct.Fields));

            _preliminaryStruct = null;
        }

        public virtual void EmitCompatibleCodeSupport()
        {
            // nop, send help ;_;
        }

        public virtual void EmitFnPtrSupport()
        {
            // nop, send help ;_;
        }

        public virtual void EmitSystemSupport()
        {
            // nop, send help ;_;
        }

        public CSharpOutputBuilder BeginCSharpCode()
        {
            return new("Whatever", _configuration);
        }

        public virtual void EndCSharpCode(CSharpOutputBuilder output)
        {
            // nop, send help ;_;
        }

        public virtual void BeginGetter(bool aggressivelyInlined)
        {
            // nop, send help ;_;
        }

        public virtual void EndGetter()
        {
            // nop, send help ;_;
        }

        public virtual void BeginSetter(bool aggressivelyInlined)
        {
            // nop, send help ;_;
        }

        public virtual void EndSetter()
        {
            // nop, send help ;_;
        }

        public virtual void BeginIndexer(AccessSpecifier accessSpecifier, bool isUnsafe)
        {
            // nop, send help ;_;
        }

        public virtual void WriteIndexer(string typeName)
        {
            // nop, send help ;_;
        }

        public virtual void BeginIndexerParameters()
        {
            // nop, send help ;_;
        }

        public virtual void EndIndexerParameters()
        {
            // nop, send help ;_;
        }

        public virtual void EndIndexer()
        {
            // nop, send help ;_;
        }

        public virtual void BeginDereference()
        {
            // nop, send help ;_;
        }

        public virtual void EndDereference()
        {
            // nop, send help ;_;
        }

        public virtual void WriteDivider(bool force = false)
        {
            // nop, send help ;_;
        }

        public virtual void SuppressDivider()
        {
            // nop, send help ;_;
        }

        public void WriteCustomAttribute(string attribute, Action callback)
        {
            // nop!
        }

        public void WriteIid(string name, Guid value)
        {
            // nop!
        }

        public virtual void WriteCustomAttribute(string attribute)
        {
            // nop, send help ;_;
        }

        public virtual void WriteIid(string iidName, string iidValue)
        {
            // nop, send help ;_;
        }

        public virtual void EmitUsingDirective(string directive)
        {
            // nop, send help
        }

        private MethodGenerationSet CreateFieldGetterMethodGenerationSet(PreliminaryStructField field)
        {
            var publicMethodName = JavaConventions.Getter(field.EscapedName, field.Type.AsString);
            var nativeMethodName = publicMethodName + "Raw";

            var generator = new ValuePassGenerator(CurrentGenerationPlan);

            generator.ConsumeStructHandle();
            generator.Consume(field.Type, "returnValue", ValuePassContext.StructFieldReturnValue);

            return BuildGenerationSet(publicMethodName, nativeMethodName,
                _preliminaryStruct.JavaType.RawName, false, generator);
        }

        private MethodGenerationSet CreateFieldSetterMethodGenerationSet(PreliminaryStructField field)
        {
            var publicMethodName = JavaConventions.Setter(field.EscapedName);
            var nativeMethodName = publicMethodName + "Raw";

            var generator = new ValuePassGenerator(CurrentGenerationPlan);

            generator.ConsumeStructHandle();
            generator.Consume(field.Type, "value", ValuePassContext.StructFieldSetterParameter);

            return BuildGenerationSet(publicMethodName, nativeMethodName,
                _preliminaryStruct.JavaType.RawName, false, generator);
        }

        private MethodGenerationSet CreateFunctionMethodGenerationSet(in PreliminaryFunction function)
        {
            var publicMethodName = function.JavaName;
            var nativeMethodName = publicMethodName + "$Raw";

            var generator = new ValuePassGenerator(CurrentGenerationPlan);

            generator.ConsumeFunctionParameters(function.Parameters, publicMethodName);

            generator.Consume(function.ReturnType, "returnValue", ValuePassContext.MethodReturnValue);

            return BuildGenerationSet(publicMethodName, nativeMethodName,
                CurrentGenerationPlan.ContainerClass, true, generator);
        }

        private static MethodGenerationSet BuildGenerationSet(
            string publicMethodName,
            string nativeMethodName,
            string containingType,
            bool isStatic,
            ValuePassGenerator generator)
        {
            var generatedParameters = generator.GeneratedParameters;
            var generatedReturnTypes = generator.GeneratedReturnTypes;

            return new MethodGenerationSet(
                new JniGlueMethod(
                    nativeMethodName,
                    generatedReturnTypes.Native,
                    generatedParameters.Native.ToImmutableArray(),
                    containingType
                ),
                new BodylessJavaMethod(
                    nativeMethodName,
                    generatedReturnTypes.InternalJavaNative,
                    generatedParameters.InternalJavaNative.ToImmutableArray()),
                new FullJavaMethod(
                    publicMethodName,
                    generatedReturnTypes.PublicJava,
                    generatedParameters.PublicJava.ToImmutableArray(),
                    isStatic
                ),
                generator.ReturnTypePass,
                generator.ParameterPasses
            );
        }

        private readonly struct PreliminaryStructField
        {
            public PreliminaryStructField(string name, TypeDesc type)
            {
                Name = name;
                EscapedName = name == "handle" ? "handleField" : name;
                Type = type;
            }

            public string Name { get; }

            /// <summary>
            /// The name for use as part of getter/setter methods
            /// </summary>
            public string EscapedName { get; }

            public TypeDesc Type { get; }
        }

        private class PreliminaryFunction
        {
            public string NativeName { get; set; }
            public string JavaName { get; set; }
            public TypeDesc ReturnType { get; set; }
            public List<MethodParameter<TypeDesc>> Parameters { get; } = new();
        }

        private class PreliminaryEnum
        {
            public string JavaName { get; set; }
            public PreliminaryEnumConstant? PreliminaryConstant { get; set; }
            public List<EnumConstantGenerationInfo> Constants { get; } = new();

            public void CommitSignedPreliminaryConstant(long enumValue)
            {
                var actualConstant = PreliminaryConstant ??
                                     throw new InvalidOperationException(
                                         "Attempted to commit while the preliminary constant is null.");

                Constants.Add(EnumConstantGenerationInfo.Signed(actualConstant.JavaName, enumValue));
            }

            public void CommitUnsignedPreliminaryConstant(ulong enumValue)
            {
                var actualConstant = PreliminaryConstant ??
                                     throw new InvalidOperationException(
                                         "Attempted to commit while the preliminary constant is null.");

                Constants.Add(EnumConstantGenerationInfo.Unsigned(actualConstant.JavaName, enumValue));
            }
        }

        private readonly struct PreliminaryEnumConstant
        {
            public PreliminaryEnumConstant(string javaName)
            {
                JavaName = javaName;
            }

            public string JavaName { get; }
        }

        private class PreliminaryStruct
        {
            public PreliminaryStruct(string javaName, ObjectJavaType javaType)
            {
                JavaName = javaName;
                JavaType = javaType;
            }

            public string JavaName { get; }
            public ObjectJavaType JavaType { get; }

            public List<StructFieldGenerationInfo> Fields { get; } = new();
        }

        protected enum BraceStyle
        {
            Allman,
            KAndR,
        }
    }
}
