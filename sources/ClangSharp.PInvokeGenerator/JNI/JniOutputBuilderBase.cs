// Copyright © Tanner Gooding and Contributors. Licensed under the MIT License (MIT). See License.md in the repository root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using ClangSharp.Abstractions;
using ClangSharp.CSharp;
using ClangSharp.JNI.Generation;
using ClangSharp.JNI.Generation.Configuration;
using ClangSharp.JNI.Generation.Enum;
using ClangSharp.JNI.Generation.Method;
using ClangSharp.JNI.Generation.Struct;
using ClangSharp.JNI.Java;
using ClangSharp.JNI.JNIGlue;

namespace ClangSharp.JNI
{
    internal abstract class JniOutputBuilderBase : IOutputBuilder, IIndentedWriter
    {
        public const string DefaultIndentationString = "    ";

        protected JniGenerationContext GenerationContext { get; }

        protected BraceStyle CurrentBraceStyle { get; set; } = BraceStyle.Allman;
        public StringBuilder RawBuilder { get; } = new();
        private int _indentationLevel = new();
        private readonly PInvokeGeneratorConfiguration _configuration;
        private readonly string _indentationString;

        private PreliminaryEnum _preliminaryEnum;
        private PreliminaryFunction _preliminaryFunction;
        private PreliminaryStruct _preliminaryStruct;

        private Queue<PreliminaryEnum> _enumQueue = new();
        private Queue<PreliminaryStruct> _structQueue = new();
        private Queue<PreliminaryFunction> _functionQueue = new();

        public bool IsUncheckedContext => true;

        protected JniOutputBuilderBase(string name, PInvokeGeneratorConfiguration configuration,
            JniGenerationContext generationContext,
            string indentationString)
        {
            Name = name;
            _configuration = configuration;
            _indentationString = indentationString;
            GenerationContext = generationContext;
        }

        public abstract string Extension { get; }
        public abstract bool IsTestOutput { get; }
        public string Name { get; }

        public string Content
        {
            get
            {
                CreateTransformationUnits();
                WriteContent();
                var content = RawBuilder.ToString();
                _ = RawBuilder.Clear();
                return content;
            }
        }

        protected abstract void WriteContent();

        public void WriteIndentation()
        {
            for (var i = 0; i < _indentationLevel; i++)
            {
                _ = RawBuilder.Append(_indentationString);
            }
        }

        public void Write(GeneratedExpression generatedExpression)
        {
            generatedExpression.WriteTo(this);
        }

        public void Write<T>(T value)
        {
            _ = RawBuilder.Append(value);
        }

        public void WriteIndentedLine(string value = "")
        {
            _ = RawBuilder.AppendLine();
            WriteIndentation();
            _ = RawBuilder.Append(value);
        }

        public void WriteNewLine()
        {
            _ = RawBuilder.AppendLine();
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

        private void CreateTransformationUnits()
        {
            GenerationContext.NewRound();

            while (_enumQueue.TryDequeue(out var @enum))
            {
                try
                {
                    var enumTarget = new EnumTarget(@enum.JavaName, @enum.Constants.ToImmutableArray());
                    GenerationContext.AddTransformationUnit(new EnumTransformationUnit(enumTarget));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to transform enum {_preliminaryEnum.JavaName}: {e.Message}");
                }
            }

            while (_structQueue.TryDequeue(out var @struct))
            {
                try
                {
                    var target = new StructTarget(@struct.NativeName, @struct.Fields.ToImmutableArray());
                    var rule = GenerationContext.Configuration.GetStructRule(target);

                    if (rule.ShouldBeGenerated)
                    {
                        var transformationUnit =
                            new StructTransformationUnit(target, rule, GenerationContext,
                                out var generatedTransformationUnits);
                        GenerationContext.AddTransformationUnit(transformationUnit);
                        GenerationContext.AddTransformationUnits(generatedTransformationUnits);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to transform struct {@struct.NativeName}: {e.Message}");
                }
            }

            while (_functionQueue.TryDequeue(out var function))
            {
                try
                {
                    var nativeMethod = new NativeMethod(function.NativeName,
                        function.ReturnType, function.Parameters.ToImmutableArray());
                    var target = new MethodTarget(nativeMethod);

                    var rule = GenerationContext.Configuration.GetMethodRule(target);

                    if (rule.ShouldBeGenerated)
                    {
                        var transformationUnit = new MethodTransformationUnit(target, rule, GenerationContext,
                            out var generatedTransformationUnits);
                        GenerationContext.AddTransformationUnit(transformationUnit);
                        GenerationContext.AddTransformationUnits(generatedTransformationUnits);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to transform function {function.NativeName}: {e.Message}");
                }
            }
        }

        // Clangsharp methods

        public CSharpOutputBuilder BeginCSharpCode()
        {
            return new("Whatever", _configuration);
        }

        public virtual void EndCSharpCode(CSharpOutputBuilder output)
        {

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
            _enumQueue.Enqueue(_preliminaryEnum);
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

            var field = new StructField(canonicalType, desc.EscapedName);

            _preliminaryStruct.Fields.Add(field);
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
                JavaName = JavaConventions.EscapeName(info.EscapedName),
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

            _functionQueue.Enqueue(_preliminaryFunction);
            _preliminaryFunction = null;
        }

        public virtual void BeginStruct(in StructDesc info)
        {
            if (info.IsComplete)
            {
                _preliminaryStruct = new PreliminaryStruct(info.EscapedName);
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

            _structQueue.Enqueue(_preliminaryStruct);
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
            public List<EnumConstant> Constants { get; } = new();

            public void CommitSignedPreliminaryConstant(long enumValue)
            {
                var actualConstant = PreliminaryConstant ??
                                     throw new InvalidOperationException(
                                         "Attempted to commit while the preliminary constant is null.");

                Constants.Add(EnumConstant.Signed(actualConstant.JavaName, enumValue));
            }

            public void CommitUnsignedPreliminaryConstant(ulong enumValue)
            {
                var actualConstant = PreliminaryConstant ??
                                     throw new InvalidOperationException(
                                         "Attempted to commit while the preliminary constant is null.");

                Constants.Add(EnumConstant.Unsigned(actualConstant.JavaName, enumValue));
            }
        }

        private readonly record struct PreliminaryEnumConstant(string JavaName);

        private class PreliminaryStruct
        {
            public PreliminaryStruct(string nativeName)
            {
                NativeName = nativeName;
            }

            public string NativeName { get; }

            public List<StructField> Fields { get; } = new();
        }


        protected enum BraceStyle
        {
            Allman,
            KAndR,
        }
    }
}
