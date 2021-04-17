using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using RoslynTestKit;
using SmartAnalyzers.CSharpExtensions.Annotations;
using static CSharpExtensions.Analyzers.Test.ConvertConstructorToInitBlock.CompleteInitializationBlockCodeFixTestsCases;

namespace CSharpExtensions.Analyzers.Test.ConvertConstructorToInitBlock
{
    public class CompleteInitializationBlockCodeFixTests : CodeFixTestFixture
    {
        protected override string LanguageName => LanguageNames.CSharp;
        protected override IReadOnlyCollection<DiagnosticAnalyzer> CreateAdditionalAnalyzers() => new[] { new RequiredPropertiesInitializationAnalyzer()
        {
            DefaultSettings = new CSE001Settings()
            {
                SkipWhenConstructorUsed = false
            }
        } };

        protected override IReadOnlyCollection<MetadataReference> References => new[]
        {
            ReferenceSource.FromType<InitRequiredAttribute>(),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location)
        };

        [Test]
        public void should_convert_constructor_to_init_block()
        {
            TestCodeFix(_001_ConstructorToInitBlock, _001_ConstructorToInitBlock_FIXED, RequiredPropertiesInitializationAnalyzer.DiagnosticId);

        }

        protected override CodeFixProvider CreateProvider() => new CompleteInitializationBlockCodeFix();

    }
}
