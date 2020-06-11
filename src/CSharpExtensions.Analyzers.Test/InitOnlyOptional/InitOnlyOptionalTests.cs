using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using RoslynTestKit;

namespace CSharpExtensions.Analyzers.Test.InitOnlyOptional
{
    public class InitOnlyOptionalTests : AnalyzerTestFixture
    {
        protected override string LanguageName => LanguageNames.CSharp;
        protected override DiagnosticAnalyzer CreateAnalyzer() => new InitOnlyOptionalAnalyzer();


        [Test]
        public void should_report_missing_property_initialization()
        {
            HasDiagnostic(InitOnlyOptionalTestCases._001_ReportMissingPropertyInitialization, InitOnlyOptionalAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_report_missing_field_initialization()
        {
            HasDiagnostic(InitOnlyOptionalTestCases._002_ReportMissingFieldInitialization, InitOnlyOptionalAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_not_report_missing_property_initialization()
        {
            NoDiagnostic(InitOnlyOptionalTestCases._003_DoNotReportMissingPropertyInitialization, InitOnlyOptionalAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_not_report_missing_field_initialization()
        {
            NoDiagnostic(InitOnlyOptionalTestCases._004_DoNotReportMissingFieldInitialization, InitOnlyOptionalAnalyzer.DiagnosticId);
        }
    }
}
