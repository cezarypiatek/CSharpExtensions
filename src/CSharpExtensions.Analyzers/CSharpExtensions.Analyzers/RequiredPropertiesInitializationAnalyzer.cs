using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpExtensions.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RequiredPropertiesInitializationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSE001";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, "Required properties initialization", "Missing initialization for properties: {0}", "CSharp Extensions", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.ObjectCreationExpression);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

            var typeInfo = context.SemanticModel.GetSymbolInfo(objectCreation.Type);

            if (typeInfo.Symbol is ITypeSymbol type)
            {
                var properties = type.GetMembers().Where(x=>x is IPropertySymbol property && property.SetMethod != null).Select(x=>x.Name).ToImmutableHashSet();
                if (properties.IsEmpty)
                {
                    return;
                }

                if (ReadonlyClassHelper.IsMarkedAsReadonly(type))
                {
                    var objectInitialization = objectCreation.Initializer;
                    var initializedProperties = objectInitialization.Expressions.OfType<AssignmentExpressionSyntax>().Select(x => x.Left)
                        .OfType<IdentifierNameSyntax>().Select(x => x.Identifier.Text).ToImmutableHashSet();

                    var missingProperties = properties.Except(initializedProperties);
                    if (missingProperties.IsEmpty == false)
                    {
                        var propertiesString = string.Join(", ", missingProperties);
                        var diagnostic = Diagnostic.Create(Rule, objectInitialization.GetLocation(), propertiesString);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}