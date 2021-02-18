using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using RoslynTestKit;

namespace CSharpExtensions.Analyzers.Test.ExpressionTooComplex
{
    public class ExpressionTooComplexTests : AnalyzerTestFixture
    {
        protected override string LanguageName => LanguageNames.CSharp;
        protected override DiagnosticAnalyzer CreateAnalyzer() => new ExpressionTooComplexAnalyzer();

        [Test]
        public void should_report_too_complex_expression()
        {
            HasDiagnostic(ExpressionTooComplexTestCases._001_TooMuchInvocationsInside, ExpressionTooComplexAnalyzer.DiagnosticId);
        }
    }
}
