using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpExtensions.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ReturnValueUnusedAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSE005";
        internal static readonly LocalizableString Title = "Return value unused";
        internal static readonly LocalizableString MessageFormat = "Use the return value or discard it explicitly";
        internal const string Category = "CSharp Extensions";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.InvocationExpression, SyntaxKind.AwaitExpression, SyntaxKind.ObjectCreationExpression);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext obj)
        {
            if (obj.Node is ExpressionSyntax expression)
            {
                if (expression.Parent is ExpressionStatementSyntax && obj.SemanticModel.GetTypeInfo(expression).Type is { } type && type.SpecialType != SpecialType.System_Void)
                {
                    var diagnostic = Diagnostic.Create(Rule, expression.GetLocation());
                    obj.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
