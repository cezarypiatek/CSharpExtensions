using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpExtensions.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ReadonlyClassFieldsSetAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSE002";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, "Readonly class fields modification", "Cannot modify fields of readonly class", "CSharp Extensions", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.SimpleAssignmentExpression);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;

            if (assignment.Left is MemberAccessExpressionSyntax memberAccess)
            {
               
                var typeInfo = context.SemanticModel.GetSymbolInfo(memberAccess.Expression);
                if (typeInfo.Symbol is ITypeSymbol type && ReadonlyClassHelper.IsMarkedAsReadonly(type))
                {
                    var diagnostic = Diagnostic.Create(Rule, assignment.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}