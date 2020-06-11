using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpExtensions.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InitOnlyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSE002";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, "InitOnly member modification", "Cannot modify InitOnly member", "CSharp Extensions", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.SimpleAssignmentExpression);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;
            if (assignment.Parent is InitializerExpressionSyntax || assignment.Left == null)
            {
                return;
            }

            var memberSymbol = context.SemanticModel.GetSymbolInfo(assignment.Left).Symbol;
            if (memberSymbol is IPropertySymbol || memberSymbol is IFieldSymbol)
            {
                if (SymbolHelper.IsMarkedWithAttribute(memberSymbol, SmartAnnotations.InitOnly) || 
                    SymbolHelper.IsMarkedWithAttribute(memberSymbol, SmartAnnotations.InitOnlyOptional) ||  
                    SymbolHelper.IsMarkedWithAttribute(memberSymbol.ContainingType, SmartAnnotations.InitOnly))
                {
                    var parentMethod = SyntaxHelper.FindNearestContainer<BaseMethodDeclarationSyntax>(assignment.Parent);
                    if (parentMethod is ConstructorDeclarationSyntax)
                    {
                        var constructorSymbol = context.SemanticModel.GetDeclaredSymbol(parentMethod);
                        if (constructorSymbol != null && constructorSymbol.ContainingType == memberSymbol.ContainingType)
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