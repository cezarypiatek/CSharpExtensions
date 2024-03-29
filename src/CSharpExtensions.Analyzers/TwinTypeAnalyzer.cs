using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpExtensions.Analyzers
{
    public class CSE003Settings
    {
        public bool IdenticalEnum { get; set; } = false;
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TwinTypeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSE003";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, "Type should have the same fields as twin type", "Missing fields from {0}:\r\n{1}", "CSharp Extensions", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public CSE003Settings DefaultSettings { get; set; }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var config = DefaultSettings ?? context.Options.GetConfigFor<CSE003Settings>(DiagnosticId, context.CancellationToken);

            if (context.Symbol is INamedTypeSymbol namedType && (namedType.TypeKind == TypeKind.Class || namedType.TypeKind == TypeKind.Struct || namedType.TypeKind == TypeKind.Enum))
            {
                foreach (var twinType in SymbolHelper.GetTwinTypes(namedType, config))
                {
                    var missingMembers = twinType.GetMissingMembersFor(namedType);
                    if (missingMembers.Count > 0)
                    {
                        var propertiesString = string.Join("\r\n", missingMembers.Select(x => x.IsEnumWithValue
                            ? $"- {x.ExpectedName} with value {x.EnumConstantValue}"
                            : $"- {x.ExpectedName}"));
                        var properties = new Dictionary<string, string>()
                        {
                            ["TwinType"] = twinType.Type.ToDisplayString()
                        };
                        var diagnostic = Diagnostic.Create(Rule, context.Symbol.Locations[0], properties.ToImmutableDictionary(), twinType.Type.ToDisplayString(), propertiesString);

                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}