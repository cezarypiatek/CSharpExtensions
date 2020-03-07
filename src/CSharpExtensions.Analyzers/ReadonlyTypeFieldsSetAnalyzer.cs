using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpExtensions.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ReadonlyTypeFieldsSetAnalyzer : DiagnosticAnalyzer
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
            if (assignment.Parent is InitializerExpressionSyntax)
            {
                return;
            }

            var memberSymbol = context.SemanticModel.GetSymbolInfo(assignment.Left).Symbol;
            if (memberSymbol != null && (memberSymbol is IPropertySymbol || memberSymbol is IFieldSymbol))
            {
                if (ReadonlyClassHelper.IsMarkedWithReadonly(memberSymbol.ContainingType))
                {
                    var parentConstructor = SyntaxHelper.FindNearestContainer<ConstructorDeclarationSyntax, TypeDeclarationSyntax>(assignment.Parent, syntax => true);
                    if (parentConstructor != null)
                    {
                        var constructorSymbol = context.SemanticModel.GetDeclaredSymbol(parentConstructor);
                        if (constructorSymbol != null && constructorSymbol.ContainingSymbol == memberSymbol.ContainingSymbol)
                        {
                            return;
                        }
                    }
                    var diagnostic = Diagnostic.Create(Rule, assignment.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}