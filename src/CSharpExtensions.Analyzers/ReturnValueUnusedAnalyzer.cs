using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
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
        public static readonly DiagnosticDescriptor ReturnValueUnused = new DiagnosticDescriptor("CSE005", "Return value unused", "Use the return value or discard it explicitly", "CSharp Extensions", DiagnosticSeverity.Warning, true);
        public static readonly DiagnosticDescriptor ReturnDisposableValueUnused = new DiagnosticDescriptor("CSE007", "Return disposable value unused", "Handle disposal correctly", "CSharp Extensions", DiagnosticSeverity.Error, true);
        public static readonly DiagnosticDescriptor ReturnAsyncResultUnused = new DiagnosticDescriptor("CSE008", "Return async result unused", "Handle async result correctly", "CSharp Extensions", DiagnosticSeverity.Error, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(ReturnValueUnused, ReturnDisposableValueUnused, ReturnAsyncResultUnused);
        
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var config = compilationContext.Options.GetConfigFor<CSE005Settings>(ReturnValueUnused.Id, compilationContext.CancellationToken);
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
                    var diagnostic = Diagnostic.Create(ReturnValueUnused, expression.GetLocation());
                    ctx.ReportDiagnostic(diagnostic);

                    if (IsAsyncResult(type) || type.AllInterfaces.Any(IsAsyncResult))
                    {
                        var asyncResultDiagnostic = Diagnostic.Create(ReturnAsyncResultUnused, expression.GetLocation());
                        ctx.ReportDiagnostic(asyncResultDiagnostic);

                    }

                    if (IsDisposable(type) || type.AllInterfaces.Any(IsDisposable))
                    {
                        var disposableDiagnostic = Diagnostic.Create(ReturnDisposableValueUnused, expression.GetLocation());
                        ctx.ReportDiagnostic(disposableDiagnostic);
                    }
                }
            }
        }

        private static bool IsDisposable(ITypeSymbol x) => x.Name is "IDisposable" or "IAsyncDisposable";
        private static bool IsAsyncResult(ITypeSymbol x) => x.Name is "IAsyncResult";
    }
}
