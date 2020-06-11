using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpExtensions.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InitOnlyOptionalAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSE004";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, "InitOnlyOptional requires default value", "Missing assignment of default value ", "CSharp Extensions", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.PropertyDeclaration, SyntaxKind.FieldDeclaration);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is PropertyDeclarationSyntax propertyDeclaration)
            {
                if (IsMarketWIthInitOnlyOptional(propertyDeclaration.AttributeLists))
                {
                    if (propertyDeclaration.Initializer?.Value == null)
                    {
                        var diagnostic = Diagnostic.Create(Rule, propertyDeclaration.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }else if (context.Node is FieldDeclarationSyntax fieldDeclaration)
            {
                if (IsMarketWIthInitOnlyOptional(fieldDeclaration.AttributeLists))
                {
                    if (fieldDeclaration.Declaration != null)
                    {
                        foreach (var variable in fieldDeclaration.Declaration.Variables)
                        {
                            if (variable.Initializer?.Value == null)
                            {
                                var diagnostic = Diagnostic.Create(Rule, variable.GetLocation());
                                context.ReportDiagnostic(diagnostic);
                            }
                        }
                    }
                }
            }
        }

        private static bool IsMarketWIthInitOnlyOptional(SyntaxList<AttributeListSyntax> propertyDeclarationAttributeLists)
        {
            return propertyDeclarationAttributeLists.SelectMany(x => x.Attributes)
                .Any(x => x.Name.ToString().EndsWith("InitOnlyOptional"));
        }
    }
}