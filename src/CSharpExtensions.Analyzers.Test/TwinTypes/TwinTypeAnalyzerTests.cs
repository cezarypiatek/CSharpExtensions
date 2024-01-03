using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using RoslynTestKit;
using SmartAnalyzers.CSharpExtensions.Annotations;
using static CSharpExtensions.Analyzers.Test.TwinTypes.ReferenceSources;

namespace CSharpExtensions.Analyzers.Test.TwinTypes
{
    public class TwinTypeAnalyzerTests
    {
        

        [Test]
        public void should_report_missing_properties()
        {
            RoslynFixtureFactory.Create<TwinTypeAnalyzer>(new AnalyzerTestFixtureConfig
            {
                References = CSEReferences
            }).HasDiagnostic(TwinTypeAnalyzerTestsTestCases._001_MissingProperties, TwinTypeAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_report_missing_fields()
        {
            RoslynFixtureFactory.Create<TwinTypeAnalyzer>(new AnalyzerTestFixtureConfig
            {
                References = CSEReferences
            }).HasDiagnostic(TwinTypeAnalyzerTestsTestCases._002_MissingFields, TwinTypeAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_report_missing_fields_for_enum()
        {
            RoslynFixtureFactory.Create<TwinTypeAnalyzer>(new AnalyzerTestFixtureConfig
            {
                References = CSEReferences
            }).HasDiagnostic(TwinTypeAnalyzerTestsTestCases._009_MissingFieldsForEnum, TwinTypeAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_not_report_wrong_fields_order_for_enum_with_correct_value()
        {
            RoslynFixtureFactory.Create<TwinTypeAnalyzer>(new AnalyzerTestFixtureConfig
            {
                References = CSEReferences
            }).NoDiagnostic(TwinTypeAnalyzerTestsTestCases._010_WrongFieldsOrderForEnumCorrectValue, TwinTypeAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_report_wrong_fields_order_for_enum_with_default_value()
        {
            RoslynFixtureFactory.Create
            (
                diagnosticAnalyzer: new TwinTypeAnalyzer
                {
                    DefaultSettings = new CSE003Settings
                    {
                        IdenticalEnum = true
                    }
                },
                config: new AnalyzerTestFixtureConfig
                {
                    References = CSEReferences
                }
            ).HasDiagnostic(TwinTypeAnalyzerTestsTestCases._011_WrongFieldsOrderForEnumDefaultValue, TwinTypeAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_report_wrong_fields_order_for_enum_with_wrong_value()
        {
            RoslynFixtureFactory.Create
            (
                diagnosticAnalyzer: new TwinTypeAnalyzer
                {
                    DefaultSettings = new CSE003Settings
                    {
                        IdenticalEnum = true
                    }
                },
                config: new AnalyzerTestFixtureConfig
                {
                    References = CSEReferences
                }
            ).HasDiagnostic(TwinTypeAnalyzerTestsTestCases._012_WrongFieldsOrderForEnumWrongValue, TwinTypeAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_report_missing_inherited_properties()
        {
            RoslynFixtureFactory.Create<TwinTypeAnalyzer>(new AnalyzerTestFixtureConfig
            {
                References = CSEReferences
            }).HasDiagnostic(TwinTypeAnalyzerTestsTestCases._003_MissingPropertiesFromInheritance, TwinTypeAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_not_report_missing_properties_if_all_types_has_the_same_members()
        {
            RoslynFixtureFactory.Create<TwinTypeAnalyzer>(new AnalyzerTestFixtureConfig
            {
                References = CSEReferences
            }).NoDiagnostic(TwinTypeAnalyzerTestsTestCases._004_NoMissingProperties, TwinTypeAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_not_report_prefixed_members_as_missing()
        {
            RoslynFixtureFactory.Create<TwinTypeAnalyzer>(new AnalyzerTestFixtureConfig
            {
                References = CSEReferences
            }).NoDiagnostic(TwinTypeAnalyzerTestsTestCases._006_PropertiesWithPrefix, TwinTypeAnalyzer.DiagnosticId);
        }
    }
}
