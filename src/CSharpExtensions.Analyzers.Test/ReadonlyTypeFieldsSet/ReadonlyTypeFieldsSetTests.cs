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
        protected override DiagnosticAnalyzer CreateAnalyzer() => new ReadonlyTypeFieldsSetAnalyzer();

        protected override IReadOnlyCollection<MetadataReference> References => new[]
        {
            ReferenceSource.FromType<ReadonlyTypeAttribute>()
        };

        [Test]
        public void should_not_report_modification_for_init_block()
        {
            NoDiagnostic(_001_DoNotReportModificationInInitBlock, ReadonlyTypeFieldsSetAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_report_modification_outside_init_block()
        {
            HasDiagnostic(_002_ReportModificationOutsideInitBlock, ReadonlyTypeFieldsSetAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_report_modification_in_own_method_via_this()
        {
            HasDiagnostic(_003_ReportModificationInOwnMethodViaThis, ReadonlyTypeFieldsSetAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_report_modification_in_own_method_without_this()
        {
            HasDiagnostic(_004_ReportModificationInOwnMethodWithoutThis, ReadonlyTypeFieldsSetAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_not_report_modification_in_own_constructor()
        {
            NoDiagnostic(_005_DoNotReportModificationInOwnConstructor, ReadonlyTypeFieldsSetAnalyzer.DiagnosticId);
        }
    }
}
