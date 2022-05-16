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
            HasDiagnostic(ReturnValueUnusedTestsTestCases._001_UnusedReturnValueOfPureMethod, ReturnValueUnusedAnalyzer.ReturnValueUnused.Id);
        }
        
        [Test]
        public void should_not_report_unused_return_value_from_void_method()
        {
            NoDiagnosticAtMarker(ReturnValueUnusedTestsTestCases._002_VoidReturnMethod, ReturnValueUnusedAnalyzer.ReturnValueUnused.Id);
        }
        
        [Test]
        public void should_report_unused_value_from_await_expression()
        {
            HasDiagnostic(ReturnValueUnusedTestsTestCases._003_AwaitExpression, ReturnValueUnusedAnalyzer.ReturnValueUnused.Id);
        }
        
        [Test]
        public void should_not_report_unused_value_from_await_task_no_value_expression()
        {
            NoDiagnostic(ReturnValueUnusedTestsTestCases._004_AwaitTaskNoValueExpression, ReturnValueUnusedAnalyzer.ReturnValueUnused.Id);
        }
        
        [Test]
        public void should_report_unused_value_from_new_object_expression()
        {
            HasDiagnostic(ReturnValueUnusedTestsTestCases._005_NewObjectExpression, ReturnValueUnusedAnalyzer.ReturnValueUnused.Id);
        }

        [Test]
        public void should_not_report_used_return_value_from_pure_function()
        {
            NoDiagnostic(ReturnValueUnusedTestsTestCases._006_UsedReturnValueOfPureMethod, ReturnValueUnusedAnalyzer.ReturnValueUnused.Id);
        }
        
        [Test]
        public void should_not_report_used_return_value_from_await_expression()
        {
            NoDiagnostic(ReturnValueUnusedTestsTestCases._007_UsedValueFromAwaitExpression, ReturnValueUnusedAnalyzer.ReturnValueUnused.Id);
        }
        
        [Test]
        public void should_not_report_used_by_if_statement()
        {
            NoDiagnostic(ReturnValueUnusedTestsTestCases._008_UsedReturnValueOfPureMethodByIfClause, ReturnValueUnusedAnalyzer.ReturnValueUnused.Id);
        }
        
        [Test]
        public void should_not_report_used_by_other_expression()
        {
            NoDiagnostic(ReturnValueUnusedTestsTestCases._009_UsedReturnValueOfPureMethodByOtherExpression, ReturnValueUnusedAnalyzer.ReturnValueUnused.Id);
        }
        
        [Test]
        public void should_not_report_used_by_return()
        {
            NoDiagnostic(ReturnValueUnusedTestsTestCases._010_UsedReturnValueOfPureMethodByReturn, ReturnValueUnusedAnalyzer.ReturnValueUnused.Id);
        }        
        
        
        [Test]
        public void should_not_report_used_by_arrow_expression()
        {
            NoDiagnostic(ReturnValueUnusedTestsTestCases._011_UsedReturnValueOfPureMethodByArrowExpression, ReturnValueUnusedAnalyzer.ReturnValueUnused.Id);
        }
        
        [Test]
        public void should_report_unused_result_from_complex_expression()
        {
            HasDiagnostic(ReturnValueUnusedTestsTestCases._012_UnusedValueFromComplexExpression, ReturnValueUnusedAnalyzer.ReturnValueUnused.Id);
        }
        
        
        [Test]
        public void should_report_unused_disposable_result()
        {
            HasDiagnostic(ReturnValueUnusedTestsTestCases._013_UnusedDirectDisposable, ReturnValueUnusedAnalyzer.ReturnDisposableValueUnused.Id);
        }
        
        
        [Test]
        public void should_report_unused_disposable_for_inherited_result()
        {
            HasDiagnostic(ReturnValueUnusedTestsTestCases._014_UnusedInheritedDisposable, ReturnValueUnusedAnalyzer.ReturnDisposableValueUnused.Id);
        }
        
        [Test]
        public void should_report_unused_disposable_for_inherited_interface_result()
        {
            HasDiagnostic(ReturnValueUnusedTestsTestCases._015_UnusedInheritedInterfaceDisposable, ReturnValueUnusedAnalyzer.ReturnDisposableValueUnused.Id);
        }
        
        [Test]
        public void should_report_unused_disposable_for_inherited_interface_while_returning_interface()
        {
            HasDiagnostic(ReturnValueUnusedTestsTestCases._016_UnusedInheritedInterfaceWhenReturningInterfaceDisposable, ReturnValueUnusedAnalyzer.ReturnDisposableValueUnused.Id);
        }
        
        [Test]
        public void should_report_unused_disposable_for_inherited_interface_and_tasks()
        {
            HasDiagnostic(ReturnValueUnusedTestsTestCases._017_UnusedInheritedInterfaceDisposableAwait, ReturnValueUnusedAnalyzer.ReturnDisposableValueUnused.Id);
        }

        [Test]
        public void should_report_unused_un_awaited_async_result()
        {
            HasDiagnostic(ReturnValueUnusedTestsTestCases._018_UnusedAsyncResult, ReturnValueUnusedAnalyzer.ReturnAsyncResultUnused.Id);
        }
        
        [Test]
        public void should_not_report_awaited_async_result()
        {
            NoDiagnostic(ReturnValueUnusedTestsTestCases._019_AwaitedAsyncResult, ReturnValueUnusedAnalyzer.ReturnAsyncResultUnused.Id);
        }
    }
}
