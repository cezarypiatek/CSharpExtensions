using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using RoslynTestKit;

namespace CSharpExtensions.Analyzers.Test.ReturnValueUnused
{
    public class ReturnValueUnusedTests : AnalyzerTestFixture
    {
        protected override string LanguageName { get; } = LanguageNames.CSharp;
        protected override DiagnosticAnalyzer CreateAnalyzer() => new ReturnValueUnusedAnalyzer();

        [Test]
        public void should_report_unused_return_value_from_pure_function()
        {
            HasDiagnostic(ReturnValueUnusedTestsTestCases._001_UnusedReturnValueOfPureMethod, ReturnValueUnusedAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_not_report_unused_return_value_from_void_method()
        {
            NoDiagnostic(ReturnValueUnusedTestsTestCases._002_VoidReturnMethod, ReturnValueUnusedAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_report_unused_value_from_await_expression()
        {
            HasDiagnostic(ReturnValueUnusedTestsTestCases._003_AwaitExpression, ReturnValueUnusedAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_not_report_unused_value_from_await_task_no_value_expression()
        {
            NoDiagnostic(ReturnValueUnusedTestsTestCases._004_AwaitTaskNoValueExpression, ReturnValueUnusedAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_report_unused_value_from_new_object_expression()
        {
            HasDiagnostic(ReturnValueUnusedTestsTestCases._005_NewObjectExpression, ReturnValueUnusedAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_not_report_used_return_value_from_pure_function()
        {
            NoDiagnostic(ReturnValueUnusedTestsTestCases._006_UsedReturnValueOfPureMethod, ReturnValueUnusedAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_not_report_used_return_value_from_await_expression()
        {
            NoDiagnostic(ReturnValueUnusedTestsTestCases._007_UsedValueFromAwaitExpression, ReturnValueUnusedAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_not_report_used_by_if_statement()
        {
            NoDiagnostic(ReturnValueUnusedTestsTestCases._008_UsedReturnValueOfPureMethodByIfClause, ReturnValueUnusedAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_not_report_used_by_other_expression()
        {
            NoDiagnostic(ReturnValueUnusedTestsTestCases._009_UsedReturnValueOfPureMethodByOtherExpression, ReturnValueUnusedAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_not_report_used_by_return()
        {
            NoDiagnostic(ReturnValueUnusedTestsTestCases._010_UsedReturnValueOfPureMethodByReturn, ReturnValueUnusedAnalyzer.DiagnosticId);
        }        
        
        
        [Test]
        public void should_not_report_used_by_arrow_expression()
        {
            NoDiagnostic(ReturnValueUnusedTestsTestCases._011_UsedReturnValueOfPureMethodByArrowExpression, ReturnValueUnusedAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_report_unused_result_from_complex_expression()
        {
            NoDiagnostic(ReturnValueUnusedTestsTestCases._012_UnusedValueFromComplexExpression, ReturnValueUnusedAnalyzer.DiagnosticId);
        }
    }
}
