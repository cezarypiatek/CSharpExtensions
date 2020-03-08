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

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RequiredPropertiesInitializationAnalyzer.Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeObjectCreationSyntax, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeObjectInitSyntax, SyntaxKind.ObjectInitializerExpression);
        }

        private void AnalyzeObjectInitSyntax(SyntaxNodeAnalysisContext context)
        {
            var initializer = (InitializerExpressionSyntax)context.Node;
            if (initializer.Parent is AssignmentExpressionSyntax assignment && assignment.Left != null)
            {

                var annotatedParent = SyntaxHelper.FindNearestContainer<ObjectCreationExpressionSyntax, MethodDeclarationSyntax>(initializer.Parent, node => IsMarkedWithComment(node, "FullInitRequired:recursive"));
                if (annotatedParent is null)
                {
                    return;
                }
                var membersExtractor = new MembersExtractor(context.SemanticModel, initializer);
                var typeInfo = context.SemanticModel.GetTypeInfo(assignment.Left);
                if (typeInfo.Type != null)
                {
                    var membersForInitialization = membersExtractor.GetAllMembersThatCanBeInitialized(typeInfo.Type).Select(x => x.Name).ToImmutableHashSet();
                    if (membersForInitialization.IsEmpty)
                    {
                        return;
                    }

                    TryToReportMissingMembers(context, initializer, membersForInitialization, initializer.GetLocation());
                }
            }
        }

        private void AnalyzeObjectCreationSyntax(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

            var typeInfo = context.SemanticModel.GetSymbolInfo(objectCreation.Type);

            if (typeInfo.Symbol is ITypeSymbol type)
            {
                var membersExtractor = new MembersExtractor(context.SemanticModel, objectCreation);
                var membersForInitialization = GetMembersForRequiredInitialization(type, objectCreation, membersExtractor).Select(x => x.Name).ToImmutableHashSet();
                if (membersForInitialization.IsEmpty)
                {
                    var annotatedParent = SyntaxHelper.FindNearestContainer<ObjectCreationExpressionSyntax, MethodDeclarationSyntax>(objectCreation.Parent, node => IsMarkedWithComment(node, "FullInitRequired:recursive"));
                    if (annotatedParent is null)
                    {
                        return;
                    }

                    membersForInitialization = membersExtractor.GetAllMembersThatCanBeInitialized(type).Select(x => x.Name).ToImmutableHashSet();
                    if (membersForInitialization.IsEmpty)
                    {
                        return;
                    }
                }

                TryToReportMissingMembers(context, objectCreation.Initializer, membersForInitialization, objectCreation.GetLocation());
            }
        }

        private static void  TryToReportMissingMembers(SyntaxNodeAnalysisContext context,
            InitializerExpressionSyntax initializer, ImmutableHashSet<string> membersForInitialization,
            Location getLocation)
        {
            var alreadyInitializedMembers = GetAlreadyInitializedMembers(initializer);
            var missingMembers = membersForInitialization.Except(alreadyInitializedMembers);
            if (missingMembers.IsEmpty == false)
            {
                var missingMembersList = string.Join("\r\n", missingMembers.Select(x => $"- {x}"));
                
                var diagnostic = Diagnostic.Create(RequiredPropertiesInitializationAnalyzer.Rule, getLocation, missingMembersList);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static IEnumerable<ISymbol> GetMembersForRequiredInitialization(ITypeSymbol type, ObjectCreationExpressionSyntax objectCreation, MembersExtractor extractor)
        {
            var members = extractor.GetAllMembersThatCanBeInitialized(type);
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
            return IsMarkedWithComment(objectCreation, "FullInitRequired") || 
                   ReadonlyClassHelper.IsMarkedWithReadonly(type) || 
                   ReadonlyClassHelper.IsMarkedWithFullInitRequired(type);
        }

        private static bool IsMarkedWithComment(ObjectCreationExpressionSyntax objectCreation, string marker)
        {
            var trivia = GetTriviaBefore(objectCreation);
            return trivia.Count > 0 && trivia.Any(x => x.Kind() == SyntaxKind.MultiLineCommentTrivia && x.ToFullString().Contains(marker));
        }

        private static SyntaxTriviaList GetTriviaBefore(ObjectCreationExpressionSyntax objectCreation)
        {

            if (objectCreation.NewKeyword.HasLeadingTrivia)
            {
                return objectCreation.NewKeyword.LeadingTrivia;
            }

            var prevToken = objectCreation.NewKeyword.GetPreviousToken();
            if (prevToken.HasTrailingTrivia)
            {
                return prevToken.TrailingTrivia;
            }
            return SyntaxTriviaList.Empty;
        }
    }
}