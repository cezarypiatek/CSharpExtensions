using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpExtensions.Analyzers
{
    //TODO: Add CodeFix for adding missing properties
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TwinTypeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSE003";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, "Type should have the same fields as twin type", "Missing fields from {0}:\r\n{1}", "CSharp Extensions", DiagnosticSeverity.Error, isEnabledByDefault: true);

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
                    var ownMembers = GetMembers(namedType);
                    var twinMembers = GetMembers(twinType.Type).Except(twinType.IgnoredMembers);
                    var missingMembers = twinMembers.Except(ownMembers);
                    if (missingMembers.IsEmpty == false)
                    {
                        var propertiesString = string.Join("\r\n", missingMembers.Select(x => $"- {x}"));
                        var diagnostic = Diagnostic.Create(Rule, context.Symbol.Locations[0], twinType.Type.ToDisplayString(), propertiesString);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static ImmutableHashSet<string> GetMembers(ITypeSymbol namedType)
        {
            return MembersExtractor.GetAllMembers(namedType)
                .Where(x => x switch
                {
                    IPropertySymbol property => property.IsStatic == false,
                    IFieldSymbol field => field.IsImplicitlyDeclared == false && field.IsStatic == false,
                    _ => false
                })
                .Select(x => x.Name)
                .ToImmutableHashSet();
        }
    }
}