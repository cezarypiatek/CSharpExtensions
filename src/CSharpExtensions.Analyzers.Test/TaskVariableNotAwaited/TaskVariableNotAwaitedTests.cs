using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using RoslynTestKit;

namespace CSharpExtensions.Analyzers.Test.TaskVariableNotAwaited
{
    internal class TaskVariableNotAwaitedTests : AnalyzerTestFixture
    {
        protected override string LanguageName { get; } = LanguageNames.CSharp;
        protected override DiagnosticAnalyzer CreateAnalyzer() => new TaskVariableNotAwaitedAnalyzer();

        [Test]
        public void should_report_un_awaited_task_variable()
        {
            HasDiagnostic(TaskVariableNotAwaiteTestCases._001_UnawaitedTask,TaskVariableNotAwaitedAnalyzer.TaskVariableNotAwaitedDescriptor.Id);
        }
        
        [Test]
        public void should_report_un_awaited_task_array()
        {
            HasDiagnostic(TaskVariableNotAwaiteTestCases._007_TaskArray,TaskVariableNotAwaitedAnalyzer.TaskVariableNotAwaitedDescriptor.Id);
        }
        
        [Test]
        public void should_not_report_awaited_task_variable()
        {
            NoDiagnosticAtMarker(TaskVariableNotAwaiteTestCases._002_AwaitedTask,TaskVariableNotAwaitedAnalyzer.TaskVariableNotAwaitedDescriptor.Id);
        }
        
        [Test]
        public void should_not_report_task_passed_to_method()
        {
            NoDiagnosticAtMarker(TaskVariableNotAwaiteTestCases._003_Task_WhenCall,TaskVariableNotAwaitedAnalyzer.TaskVariableNotAwaitedDescriptor.Id);
        }
        [Test]
        public void should_not_report_task_sync_wait()
        {
            NoDiagnosticAtMarker(TaskVariableNotAwaiteTestCases._004_SyncWait,TaskVariableNotAwaitedAnalyzer.TaskVariableNotAwaitedDescriptor.Id);
        }
        
        [Test]
        public void should_not_report_when_await_mixed_array()
        {
            NoDiagnosticAtMarker(TaskVariableNotAwaiteTestCases._005_AwaitMixed,TaskVariableNotAwaitedAnalyzer.TaskVariableNotAwaitedDescriptor.Id);
        }
        
        [Test]
        public void should_not_report_when_passed_to_method()
        {
            NoDiagnosticAtMarker(TaskVariableNotAwaiteTestCases._006_PassedToMethod,TaskVariableNotAwaitedAnalyzer.TaskVariableNotAwaitedDescriptor.Id);
        }
        
        [Test]
        public void should_not_report_when_assigned_to_other_variable()
        {
            NoDiagnosticAtMarker(TaskVariableNotAwaiteTestCases._008_AssignedToOther,TaskVariableNotAwaitedAnalyzer.TaskVariableNotAwaitedDescriptor.Id);
        }
        
        [Test]
        public void should_not_report_returned_task()
        {
            NoDiagnosticAtMarker(TaskVariableNotAwaiteTestCases._009_ReturnedTask,TaskVariableNotAwaitedAnalyzer.TaskVariableNotAwaitedDescriptor.Id);
        }
        
        [Test]
        public void should_not_report_awaited_in_lambda()
        {
            NoDiagnosticAtMarker(TaskVariableNotAwaiteTestCases._010_Awaited_In_Lambda,TaskVariableNotAwaitedAnalyzer.TaskVariableNotAwaitedDescriptor.Id);
        }
    }
}
