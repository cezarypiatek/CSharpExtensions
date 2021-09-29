using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CSharpExtensions.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpExtensions.Analyzers3
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RequiredPropertiesInitializationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CSE001";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, "Required properties initialization", "Missing initialization for properties:\r\n{0}", "CSharp Extensions", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RequiredPropertiesInitializationAnalyzer.Rule);

        public CSE001Settings DefaultSettings { get; set; }
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(compilationContext =>
            {
                if (LanguageFeaturesAvailability.ImplicitObjectCreation)
                {
                    var config = DefaultSettings ?? compilationContext.Options.GetConfigFor<CSE001Settings>(DiagnosticId, compilationContext.CancellationToken);

                    compilationContext.RegisterSyntaxNodeAction(analysisContext => AnalyzeObjectCreationSyntax(analysisContext, config), SyntaxKind.ImplicitObjectCreationExpression);
                    compilationContext.RegisterSyntaxNodeAction(analysisContext => AnalyzeObjectCreationSyntax(analysisContext, config), SyntaxKind.ObjectCreationExpression);
                    compilationContext.RegisterSyntaxNodeAction(analysisContext => AnalyzeObjectInitSyntax(analysisContext, config), SyntaxKind.ObjectInitializerExpression);
                }
            });
        }

        private void AnalyzeObjectInitSyntax(SyntaxNodeAnalysisContext context, CSE001Settings cse001Settings)
        {
            var initializer = (InitializerExpressionSyntax)context.Node;
            if (initializer.Parent is AssignmentExpressionSyntax { Left: { } } assignment)
            {

                var annotatedParent = SyntaxHelper.FindNearestContainer<BaseObjectCreationExpressionSyntax, MethodDeclarationSyntax>(initializer.Parent, node => IsMarkedWithComment(node, "FullInitRequired:recursive"));
                if (annotatedParent is null)
                {
                    return;
                }

                var typeInfo = context.SemanticModel.GetTypeInfo(assignment.Left);
                if (typeInfo.Type != null)
                {
                    var membersExtractor = new MembersExtractor(context.SemanticModel, initializer);
                    var membersForInitialization = membersExtractor.GetAllMembersThatCanBeInitialized(typeInfo.Type).Select(x => x.Name).ToImmutableHashSet();
                    if (membersForInitialization.IsEmpty)
                    {
                        return;
                    }

                    TryToReportMissingMembers(context, initializer, membersForInitialization, initializer.GetLocation());
                }
            }
        }

        private void AnalyzeObjectCreationSyntax(SyntaxNodeAnalysisContext context, CSE001Settings settings)
        {
            var objectCreation = (BaseObjectCreationExpressionSyntax)context.Node;

            if (objectCreation.ArgumentList?.Arguments.Any() == true && settings.SkipWhenConstructorUsed)
            {
                return;
            }

            var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation);

            if (typeInfo.Type is ITypeSymbol type)
            {
                var membersForInitialization = GetMembersForRequiredInit(type, objectCreation, context.SemanticModel).Select(x => x.Name).ToImmutableHashSet();
                if (membersForInitialization.IsEmpty)
                {
                    return;
                }

                TryToReportMissingMembers(context, objectCreation.Initializer, membersForInitialization, objectCreation.GetLocation());
            }
        }

        public static IEnumerable<ISymbol> GetMembersForRequiredInit(ITypeSymbol type, BaseObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel)
        {
            var membersExtractor = new MembersExtractor(semanticModel, objectCreation);
            if (IsInsideInitBlockWithFullInit(objectCreation) ||
                SymbolHelper.IsMarkedWithAttribute(type, SmartAnnotations.InitRequired) ||
                SymbolHelper.IsMarkedWithAttribute(type, SmartAnnotations.InitOnly))
            {
               return membersExtractor.GetAllMembersThatCanBeInitialized(type)
                   .Where(x => (SymbolHelper.IsMarkedWithAttribute(x, SmartAnnotations.InitOnlyOptional) == false) &&
                               (SymbolHelper.IsMarkedWithAttribute(x, SmartAnnotations.InitOptional) == false) &&
                               (NullableHelper.IsNullableMember(x) == false));
            }

            var symbolCache = new SymbolHelperCache();
            return membersExtractor.GetAllMembersThatCanBeInitialized(type).Where(memberSymbol =>
                SymbolHelper.IsMarkedWithAttribute(memberSymbol, SmartAnnotations.InitRequired) ||
                SymbolHelper.IsMarkedWithAttribute(memberSymbol, SmartAnnotations.InitOnly) ||
                NonNullableShouldBeInitialized(memberSymbol, symbolCache));
        }

        private static bool IsInsideInitBlockWithFullInit(BaseObjectCreationExpressionSyntax objectCreation)
        {
            if (IsMarkedWithComment(objectCreation, "FullInitRequired"))
            {
                return true;
            }

            var annotatedParent = SyntaxHelper.FindNearestContainer<BaseObjectCreationExpressionSyntax, MethodDeclarationSyntax>(objectCreation.Parent, node => IsMarkedWithComment(node, "FullInitRequired:recursive"));
            return annotatedParent is null == false;
        }

        private static void  TryToReportMissingMembers(SyntaxNodeAnalysisContext context, InitializerExpressionSyntax initializer, ImmutableHashSet<string> membersForInitialization, Location getLocation)
        {
            var alreadyInitializedMembers = GetAlreadyInitializedMembers(initializer);
            var missingMembers = membersForInitialization.Except(alreadyInitializedMembers);
            if (missingMembers.IsEmpty == false)
            {
                var missingMembersList = string.Join("\r\n", missingMembers.Select(x => $"- {x}"));
                
                var diagnostic = Diagnostic.Create(Rule, getLocation, missingMembersList);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool NonNullableShouldBeInitialized(ISymbol member, SymbolHelperCache symbolHelperCache) => 
            symbolHelperCache.IsMarkedWithAttribute(member.ContainingAssembly, SmartAnnotations.InitRequiredForNotNull) && 
                (
                    SymbolHelper.IsMarkedWithAttribute(member.ContainingType, SmartAnnotations.InitOptional) == false &&
                    NullableHelper.IsNotNullable(member) && 
                    SymbolHelper.IsMarkedWithAttribute(member, SmartAnnotations.InitOptional) == false &&
                    SymbolHelper.IsMarkedWithAttribute(member, SmartAnnotations.InitOnlyOptional) == false
                 );

        public static ImmutableHashSet<string> GetAlreadyInitializedMembers(InitializerExpressionSyntax objectInitialization)
        {
            if (objectInitialization?.Expressions == null)
            {
                return ImmutableHashSet<string>.Empty;
            }

            return objectInitialization.Expressions.OfType<AssignmentExpressionSyntax>().Select(x => x.Left)
                .OfType<IdentifierNameSyntax>().Select(x => x.Identifier.Text).ToImmutableHashSet();
        }

        private static bool IsMarkedWithComment(BaseObjectCreationExpressionSyntax objectCreation, string marker)
        {
            var trivia = GetTriviaBefore(objectCreation);
            return trivia.Count > 0 && trivia.Any(x => x.Kind() == SyntaxKind.MultiLineCommentTrivia && x.ToFullString().Contains(marker));
        }

        private static SyntaxTriviaList GetTriviaBefore(BaseObjectCreationExpressionSyntax objectCreation)
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