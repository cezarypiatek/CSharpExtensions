using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace CSharpExtensions.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExpressionTooComplexCodeFix)), Shared]
    public class ExpressionTooComplexCodeFix : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ExpressionTooComplexAnalyzer.DiagnosticId);
        private const string Title = "Untangle complex expression";
        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {

            var diagnostic = context.Diagnostics[0];
            context.RegisterCodeFix(CodeAction.Create(title: Title, createChangedDocument: c => UntangleComplexExpression(context.Document, diagnostic, context), equivalenceKey: Title), diagnostic);
        }

        private async Task<Document> UntangleComplexExpression(Document contextDocument, Diagnostic diagnostic, CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);
            var token = root.FindToken(diagnostic.Location.SourceSpan.Start);

            if (FindStatementToReplace(token.Parent) is {} expression)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(expression);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    var argumentList = expression switch
                    {
                        InvocationExpressionSyntax invocationExpression => invocationExpression.ArgumentList,
                        ObjectCreationExpressionSyntax objectCreationExpression => objectCreationExpression.ArgumentList,
                        _ => null
                    };

                    if (argumentList?.Arguments is { } arguments && arguments.Count > 0)
                    {
                        var extractedVariables = new List<SyntaxNode>();
                        var newArguments = new List<ArgumentSyntax>();
                        var parameterIndex = 0;
                        for (int argumentIndex = 0; argumentIndex < arguments.Count; argumentIndex++)
                        {
                            var argument = arguments[argumentIndex];
                            var isParams = methodSymbol.Parameters[parameterIndex].IsParams;

                            if (ExpressionTooComplexAnalyzer.IsNonTrivialExpression(argument))
                            {
                                var parameterName = methodSymbol.Parameters[parameterIndex].Name;
                                if (isParams)
                                {
                                    parameterName += (argumentIndex - parameterIndex + 1);
                                }

                                var extractedVariable = ExtractedVariable(parameterName, argument);
                                extractedVariables.Add(extractedVariable);
                                newArguments.Add(argument.WithExpression(IdentifierName(parameterName)));
                            }
                            else
                            {
                                newArguments.Add(argument);
                            }

                            
                            if (isParams == false)
                            {
                                parameterIndex++;
                            }
                        }
                      
                        var newExpression = expression.ReplaceNode(argumentList, ArgumentList(new SeparatedSyntaxList<ArgumentSyntax>().AddRange(newArguments)));
                        var rootStatement = expression.FirstAncestorOrSelf<StatementSyntax>();
                        extractedVariables.Add(rootStatement.ReplaceNode(expression, newExpression));
                        return await ReplaceNodes(contextDocument, rootStatement, extractedVariables, context.CancellationToken);
                    }

                }
            }

            return contextDocument;
        }

        private static LocalDeclarationStatementSyntax ExtractedVariable(string parameterName, ArgumentSyntax argument)
        {
            return LocalDeclarationStatement(VariableDeclaration(IdentifierName("var")).WithVariables(
                SingletonSeparatedList(VariableDeclarator(Identifier(parameterName))
                    .WithInitializer(EqualsValueClause(argument.Expression.WithAdditionalAnnotations(Formatter.Annotation))))));
        }

        private static async Task<Document> ReplaceNodes(Document document, SyntaxNode oldNode, IReadOnlyList<SyntaxNode> newNodes, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(oldNode, newNodes);
            return document.WithSyntaxRoot(newRoot);
        }
        private SyntaxNode FindStatementToReplace(SyntaxNode node)
        {
            return node switch
            {
                ObjectCreationExpressionSyntax objectCreation=> objectCreation,
                InvocationExpressionSyntax invocation=> invocation,
                MethodDeclarationSyntax _ => null,
                _ => node.Parent == null ? null : FindStatementToReplace(node.Parent),
            };
        }
    }
}
