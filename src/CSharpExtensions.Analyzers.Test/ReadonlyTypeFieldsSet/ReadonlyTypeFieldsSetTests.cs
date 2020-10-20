using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using RoslynTestKit;
using SmartAnalyzers.CSharpExtensions.Annotations;
using static CSharpExtensions.Analyzers.Test.ReadonlyTypeFieldsSet.ReadonlyTypeFieldsSetTestCases;

namespace CSharpExtensions.Analyzers.Test.ReadonlyTypeFieldsSet
{
    public class ReadonlyTypeFieldsSetTests : AnalyzerTestFixture
    {
        protected override string LanguageName => LanguageNames.CSharp;
        protected override DiagnosticAnalyzer CreateAnalyzer() => new InitOnlyAnalyzer();

        protected override IReadOnlyCollection<MetadataReference> References => new[]
        {
            ReferenceSource.FromType<InitOnlyAttribute>()
        };

        [Test]
        public void should_not_report_modification_for_init_block()
        {
            NoDiagnostic(_001_DoNotReportModificationInInitBlock, InitOnlyAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_report_modification_outside_init_block()
        {
            HasDiagnostic(_002_ReportModificationOutsideInitBlock, InitOnlyAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_report_modification_in_own_method_via_this()
        {
            HasDiagnostic(_003_ReportModificationInOwnMethodViaThis, InitOnlyAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_report_modification_in_own_method_without_this()
        {
            HasDiagnostic(_004_ReportModificationInOwnMethodWithoutThis, InitOnlyAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_not_report_modification_in_own_constructor()
        {
            NoDiagnostic(_005_DoNotReportModificationInOwnConstructor, InitOnlyAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_report_modification_for_init_only_property()
        {
            HasDiagnostic(_006_ReportModificationOutsideInitBlockForProperty, InitOnlyAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_report_modification_for_non_init_only_property()
        {
            NoDiagnostic(_007_DoNotReportModificationOutsideInitBlockForNonInitOnly, InitOnlyAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_report_modification_for_init_only_optional()
        {
            HasDiagnostic(_008_ReportModificationForInitOnlyOptional, InitOnlyAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_not_report_modification_for_init_only_optional_in_member_declaration()
        {
            NoDiagnostic(_009_DoNotReportInitOnlyOptionalModifucationInFieldDefinition, InitOnlyAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_not_report_modification_in_own_property()
        {
            NoDiagnostic(_010_DoNotReportModificationInOwnProperty, InitOnlyAnalyzer.DiagnosticId);
        }
    }
}
