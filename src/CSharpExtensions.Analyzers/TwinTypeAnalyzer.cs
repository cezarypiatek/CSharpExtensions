using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpExtensions.Analyzers
{
    //TODO: Add option for ignoring properties types
    //TODO: Add CodeFix for adding missing properties
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TwinTypeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSE003";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, "Type should have the same fields as twin type", "Missing properties from {0}: {1}", "CSharp Extensions", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            if(context.Symbol is INamedTypeSymbol namedType)
            {
                foreach (var twinType in SymbolHelper.GetTwinTypes(namedType))
                {
                    var ownProperties = GetProperties(namedType);
                    var twinProperties = GetProperties(twinType.Type).Except(twinType.IgnoredMembers);
                    var missingProperties = twinProperties.Except(ownProperties);
                    if (missingProperties.IsEmpty == false)
                    {
                        var propertiesString = string.Join(", ", missingProperties);
                        var diagnostic = Diagnostic.Create(Rule, context.Symbol.Locations[0], twinType.Type.ToDisplayString(), propertiesString);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static ImmutableHashSet<string> GetProperties(ITypeSymbol namedType)
        {
            return namedType.GetMembers().Where(x => x is IPropertySymbol).Select(x => x.Name).ToImmutableHashSet();
        }
    }
}