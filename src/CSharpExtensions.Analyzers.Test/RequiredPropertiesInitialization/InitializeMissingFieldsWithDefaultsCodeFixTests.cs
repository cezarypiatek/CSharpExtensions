using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using RoslynTestKit;
using SmartAnalyzers.CSharpExtensions.Annotations;
using static CSharpExtensions.Analyzers.Test.RequiredPropertiesInitialization.RequiredPropertiesInitializationTestCases;


namespace CSharpExtensions.Analyzers.Test.RequiredPropertiesInitialization
{
    public class InitializeMissingFieldsWithDefaultsCodeFixTests : CodeFixTestFixture
    {
        protected override string LanguageName { get; } = LanguageNames.CSharp;
        protected override CodeFixProvider CreateProvider() => new InitializeMissingFieldsWithDefaultsCodeFix();
        protected override IReadOnlyCollection<DiagnosticAnalyzer> CreateAdditionalAnalyzers() => new[] { new RequiredPropertiesInitializationAnalyzer() };
        protected override IReadOnlyCollection<MetadataReference> References => new[]
        {
            ReferenceSource.FromType<TwinTypeAttribute>(),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.0.0.0").Location)
        };

        [Test]
        public void should_add_missing_initializations()
        {
            TestCodeFix(_001_MissingPropertiesFullInitRequiredAttribute, _001_MissingPropertiesFullInitRequiredAttribute_FIXED, RequiredPropertiesInitializationAnalyzer.DiagnosticId);
        }
        
        
        [Test]
        public void should_add_missing_initializations_when_all_missing()
        {
            TestCodeFix(_014_MissingPropertiesFullInitRequiredAttributeAll, _014_MissingPropertiesFullInitRequiredAttributeAll_FIXED, RequiredPropertiesInitializationAnalyzer.DiagnosticId);
        }
    }
}
