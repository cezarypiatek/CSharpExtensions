using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpExtensions.Analyzers
{
    public class CSE005Settings
    {
        public IReadOnlyList<string> IgnoredReturnTypes { get; set; } = Array.Empty<string>();
    }

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
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var config = compilationContext.Options.GetConfigFor<CSE005Settings>(DiagnosticId, compilationContext.CancellationToken);
                compilationContext.RegisterSyntaxNodeAction(ctx => AnalyzeSyntax(ctx, config), SyntaxKind.InvocationExpression, SyntaxKind.AwaitExpression, SyntaxKind.ObjectCreationExpression);
            });
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext ctx, CSE005Settings settings)
        {
            if (ctx.Node is ExpressionSyntax expression)
            {
                if (expression.Parent is ExpressionStatementSyntax && ctx.SemanticModel.GetTypeInfo(expression).Type is { } type && type.SpecialType != SpecialType.System_Void)
                {
                    var fullName = type.ToDisplayString();
                    if (settings.IgnoredReturnTypes.Any(x => fullName.StartsWith(x)))
                    {
                        return;
                    }

                    var diagnostic = Diagnostic.Create(Rule, expression.GetLocation());
                    ctx.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
