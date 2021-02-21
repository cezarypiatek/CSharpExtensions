using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using RoslynTestKit;

namespace CSharpExtensions.Analyzers.Test.ExpressionTooComplex
{
    public class ExpressionTooComplexCodeFixTests : CodeFixTestFixture
    {
        protected override string LanguageName => LanguageNames.CSharp;

        
        protected override IReadOnlyCollection<DiagnosticAnalyzer> CreateAdditionalAnalyzers() => new DiagnosticAnalyzer[]
        {
            new ExpressionTooComplexAnalyzer()
        };

        [Test]
        public void should_report_too_complex_expression()
        {
            TestCodeFix(ExpressionTooComplexTestCases._001_TooMuchInvocationsInside, ExpressionTooComplexTestCases._001_TooMuchInvocationsInside_FIXED, ExpressionTooComplexAnalyzer.DiagnosticId);
        }

        protected override CodeFixProvider CreateProvider() => new ExpressionTooComplexCodeFix();
    }
}
