using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FlowAnalysis;
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
                            IEnumerable<IOperation> EnumerateOperations(IEnumerable<IOperation> input, ControlFlowGraph currentControlFlowGraph)
                            {
                                
                                foreach (var operation in input)
                                {
                                    if (operation == null)
                                    {
                                        continue;
                                    }

                                    yield return operation;

                                    if (operation is IFlowAnonymousFunctionOperation lambdaOperation)
                                    {
                                       var lambdaFlow = currentControlFlowGraph.GetAnonymousFunctionControlFlowGraph(lambdaOperation);
                                       if (lambdaFlow != null)
                                       {
                                           foreach (var lambdaSubOperation in EnumerateOperations(lambdaFlow.Blocks.SelectMany(c => c.Operations.Add(c.BranchValue)), lambdaFlow))
                                           {
                                               yield return lambdaSubOperation;
                                           }
                                       }
                                    }
                                    foreach (var childOperation in EnumerateOperations(operation.Children, currentControlFlowGraph))
                                    {
                                        yield return childOperation;
                                    }
                                }
                            }

                            var controlFlowGraph = actionContext.GetControlFlowGraph();

                            foreach (var operation in EnumerateOperations(controlFlowGraph.Blocks.SelectMany(c => c.Operations.Add(c.BranchValue)), controlFlowGraph))
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

        private static ControlFlowGraph GetControlFlow(ControlFlowGraph context, IFlowAnonymousFunctionOperation lambdaOperation)
        {
            return context.GetAnonymousFunctionControlFlowGraph(lambdaOperation);
        }

        private static bool IsTask(ITypeSymbol variableType)
        {
            return variableType is {Name: "Task" or "ValueTask", ContainingNamespace: {Name: "Tasks", ContainingNamespace:{Name: "Threading" } }};
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(TaskVariableNotAwaitedDescriptor);
    }
}