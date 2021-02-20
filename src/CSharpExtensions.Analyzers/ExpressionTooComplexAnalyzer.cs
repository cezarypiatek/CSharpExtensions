using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpExtensions.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExpressionTooComplexAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSE006";
        internal static readonly LocalizableString Title = "Expression too complex";
        internal static readonly LocalizableString MessageFormat = "Try to simplify expression by extracting method invocation results to separated variables";
        internal const string Category = "CSharp Extensions";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression, SyntaxKind.ObjectCreationExpression);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext ctx)
        {
            if (ctx.Node is ExpressionSyntax expression )
            {
                if (expression.Parent is ArgumentSyntax)
                {
                    return;
                }

                var argumentList = expression switch
                {
                    InvocationExpressionSyntax invocationExpression => invocationExpression.ArgumentList,
                    ObjectCreationExpressionSyntax objectCreationExpression => objectCreationExpression.ArgumentList,
                    _ => null
                };

                if (argumentList?.Arguments is { } arguments && arguments.Count > 0)
                {
                    foreach (var argument in arguments)
                    {
                        if (IsNonTrivialExpression(argument))
                        {
                            var location = expression switch
                            {
                                InvocationExpressionSyntax invocationExpression when invocationExpression.Expression is MemberAccessExpressionSyntax ma => ma.Name.GetLocation(),
                                _ => expression.GetLocation()
                            };
                            var diagnostic = Diagnostic.Create(Rule, location);
                            ctx.ReportDiagnostic(diagnostic);
                            return;
                        }
                    }
                }

            }
        }

        public static bool IsNonTrivialExpression(ArgumentSyntax argument)
        {
            if (argument.Expression is  InvocationExpressionSyntax invocationExpression)
            {
                if (invocationExpression.Expression is IdentifierNameSyntax identifierNameSyntax && identifierNameSyntax.Identifier.Text is "nameof" or "typeof")
                {
                    return false;
                }
                return true;
            }


            if (argument.Expression is ObjectCreationExpressionSyntax or AwaitExpressionSyntax or ConditionalExpressionSyntax)
                return true;

            if (argument.Expression is BinaryExpressionSyntax binaryExpression)
            {
                if (binaryExpression.Left is InvocationExpressionSyntax or ObjectCreationExpressionSyntax or AwaitExpressionSyntax or ConditionalExpressionSyntax or BinaryExpressionSyntax or ParenthesizedExpressionSyntax)
                {
                    return true;
                }
                
                if (binaryExpression.Right is InvocationExpressionSyntax or ObjectCreationExpressionSyntax or AwaitExpressionSyntax or ConditionalExpressionSyntax or BinaryExpressionSyntax or ParenthesizedExpressionSyntax)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
