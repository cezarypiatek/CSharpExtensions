using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace CSharpExtensions.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TaskVariableNotAwaitedAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor TaskVariableNotAwaitedDescriptor = new DiagnosticDescriptor("CSE009", "Task variable not awaited", "Task assigned to a variable is not awaited", "CSharp Extensions", DiagnosticSeverity.Warning, true);
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterOperationAction
            (
                action: actionContext =>
                {
                    if (actionContext.Operation is IVariableDeclaratorOperation variableDeclaratorOperation)
                    {
                        var variableType = variableDeclaratorOperation.Symbol.Type;
                        if (IsTask(variableType) || (variableType is IArrayTypeSymbol arrayTypeSymbol && IsTask(arrayTypeSymbol.ElementType)))
                        {
                            IEnumerable<IOperation> EnumerateOperations(IEnumerable<IOperation> input)
                            {
                                foreach (var operation in input)
                                {
                                    yield return operation;
                                    foreach (var childOperation in EnumerateOperations(operation.Children))
                                    {
                                        yield return childOperation;
                                    }
                                }
                            }

                            foreach (var operation in EnumerateOperations(actionContext.GetControlFlowGraph().Blocks.SelectMany(c => c.Operations)))
                            {
                                if (operation is ILocalReferenceOperation referenceOperation && referenceOperation.Local == variableDeclaratorOperation.Symbol)
                                {
                                    if (operation.Parent is ISimpleAssignmentOperation assignmentOperation && assignmentOperation.Value == operation)
                                    {
                                        return;
                                    }

                                    if (operation.Parent is IReturnOperation or IAwaitOperation or IArrayInitializerOperation or IArgumentOperation or IConversionOperation)
                                    {
                                        return;
                                    }

                                    if (operation.Parent is IInvocationOperation invocationOperation && invocationOperation.TargetMethod.Name is "Wait" or "GetAwaiter")
                                    {
                                        return;
                                    }
                                }
                            }

                            actionContext.ReportDiagnostic(Diagnostic.Create(TaskVariableNotAwaitedDescriptor, variableDeclaratorOperation.Syntax.GetLocation()));
                        }
                    }
                },
                operationKinds: OperationKind.VariableDeclarator
            );
        }

        private static bool IsTask(ITypeSymbol variableType)
        {
            return variableType is {Name: "Task" or "ValueTask", ContainingNamespace: {Name: "Tasks", ContainingNamespace:{Name: "Threading" } }};
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(TaskVariableNotAwaitedDescriptor);
    }
}