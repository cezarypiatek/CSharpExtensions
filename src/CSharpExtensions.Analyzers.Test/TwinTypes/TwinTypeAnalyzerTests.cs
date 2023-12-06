using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using RoslynTestKit;
using SmartAnalyzers.CSharpExtensions.Annotations;

namespace CSharpExtensions.Analyzers.Test.TwinTypes
{
    public class TwinTypeAnalyzerTests : AnalyzerTestFixture
    {
        protected override string LanguageName { get; } = LanguageNames.CSharp;
        protected override DiagnosticAnalyzer CreateAnalyzer() => new TwinTypeAnalyzer();

        protected override IReadOnlyCollection<MetadataReference> References => new[]
        {
            ReferenceSource.FromType<TwinTypeAttribute>(),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.1.0.0").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=6.0.0.0").Location)
        };

        [Test]
        public void should_report_missing_properties()
        {
            HasDiagnostic(TwinTypeAnalyzerTestsTestCases._001_MissingProperties, TwinTypeAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_report_missing_fields()
        {
            HasDiagnostic(TwinTypeAnalyzerTestsTestCases._002_MissingFields, TwinTypeAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_report_missing_fields_for_enum()
        {
            HasDiagnostic(TwinTypeAnalyzerTestsTestCases._009_MissingFieldsForEnum, TwinTypeAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_report_wrong_fields_order_for_enum()
        {
            HasDiagnostic(TwinTypeAnalyzerTestsTestCases._010_WrongFieldsOrderForEnum, TwinTypeAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_report_missing_inherited_properties()
        {
            HasDiagnostic(TwinTypeAnalyzerTestsTestCases._003_MissingPropertiesFromInheritance, TwinTypeAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_not_report_missing_properties_if_all_types_has_the_same_members()
        {
            NoDiagnostic(TwinTypeAnalyzerTestsTestCases._004_NoMissingProperties, TwinTypeAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_not_report_prefixed_members_as_missing()
        {
            NoDiagnostic(TwinTypeAnalyzerTestsTestCases._006_PropertiesWithPrefix, TwinTypeAnalyzer.DiagnosticId);
        }

    }

    public class TwinTypeCodeFixTests : CodeFixTestFixture
    {
        protected override string LanguageName { get; } = LanguageNames.CSharp;
        protected override CodeFixProvider CreateProvider() => new AddMissingMembersOfTwinTypeCodeFixProvider();
        protected override IReadOnlyCollection<DiagnosticAnalyzer> CreateAdditionalAnalyzers() => new[] { new TwinTypeAnalyzer() };
        protected override IReadOnlyCollection<MetadataReference> References => new[]
        {
            ReferenceSource.FromType<TwinTypeAttribute>(),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.1.0.0").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=6.0.0.0").Location)
        };


        [Test]
        public void should_add_missing_properties_and_fields()
        {
            TestCodeFix(TwinTypeAnalyzerTestsTestCases._005_MissingMembersForFIx, TwinTypeAnalyzerTestsTestCases._005_MissingMembersForFIx_FIXED, TwinTypeAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_add_missing_fields_for_enum()
        {
            TestCodeFix(TwinTypeAnalyzerTestsTestCases._009_MissingFieldsForEnum, TwinTypeAnalyzerTestsTestCases._009_MissingFieldsForEnum_FIXED, TwinTypeAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_add_missing_properties_with_prefix()
        {
            TestCodeFix(TwinTypeAnalyzerTestsTestCases._007_PropertiesWithPrefixForFix, TwinTypeAnalyzerTestsTestCases._007_PropertiesWithPrefixForFix_FIXED, TwinTypeAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_add_missing_properties_with_prefix_from_second_twin()
        {
            TestCodeFix(TwinTypeAnalyzerTestsTestCases._008_PropertiesWithPrefixWithTwoTwinsForFix, TwinTypeAnalyzerTestsTestCases._008_PropertiesWithPrefixWithTwoTwinsForFix_FIXED, TwinTypeAnalyzer.DiagnosticId, 1);
        }
    }
}
