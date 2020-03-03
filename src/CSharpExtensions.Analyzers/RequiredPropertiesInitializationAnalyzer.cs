using System.Collections.Generic;
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

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, "Required properties initialization", "Missing initialization for properties:\r\n{0}", "CSharp Extensions", DiagnosticSeverity.Error, isEnabledByDefault: true);

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
                var membersForInitialization = GetMembersForRequiredInitialization(type, objectCreation).Select(x => x.Name).ToImmutableHashSet();
                if (membersForInitialization.IsEmpty)
                {
                    return;
                }

                var objectInitialization = objectCreation.Initializer;
                var alreadyInitializedMembers = GetAlreadyInitializedMembers(objectInitialization);

                var missingMembers = membersForInitialization.Except(alreadyInitializedMembers);
                if (missingMembers.IsEmpty == false)
                {
                    var missingMembersList = string.Join("\r\n", missingMembers.Select(x => $"- {x}"));
                    var diagnostic = Diagnostic.Create(Rule, objectCreation.GetLocation(), missingMembersList);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static IEnumerable<ISymbol> GetMembersForRequiredInitialization(ITypeSymbol type, ObjectCreationExpressionSyntax objectCreation)
        {
            var members = type.GetMembers().Where(x => x is IPropertySymbol property && property.SetMethod != null);
            if (IsFullInitRequired(type, objectCreation))
            {
                return members;
            }
            return members.Where(x=> ReadonlyClassHelper.IsMarkedWithAttribute(x, "InitRequiredAttribute"));
        }

        private static ImmutableHashSet<string> GetAlreadyInitializedMembers(InitializerExpressionSyntax objectInitialization)
        {
            if (objectInitialization?.Expressions == null)
            {
                return ImmutableHashSet<string>.Empty;
            }

            return objectInitialization.Expressions.OfType<AssignmentExpressionSyntax>().Select(x => x.Left)
                .OfType<IdentifierNameSyntax>().Select(x => x.Identifier.Text).ToImmutableHashSet();
        }

        private static bool IsFullInitRequired(ITypeSymbol type, ObjectCreationExpressionSyntax objectCreation)
        {

            if (objectCreation.HasLeadingTrivia)
            {
                if (objectCreation.NewKeyword.LeadingTrivia.Any(x => x.Kind() == SyntaxKind.MultiLineCommentTrivia && x.ToFullString().Contains("FullInitRequired")))
                {
                    return true;
                }
            }
            return ReadonlyClassHelper.IsMarkedWithReadonly(type) || ReadonlyClassHelper.IsMarkedWithFullInitRequired(type);
        }
    }
}