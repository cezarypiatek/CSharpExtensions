using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CSharpExtensions.Analyzers;
using Microsoft.CodeAnalysis.Formatting;

namespace CSharpExtensions.Analyzers3
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InitializeMissingFieldsWithDefaultsCodeFix)), Shared]
    public class InitializeMissingFieldsWithDefaultsCodeFix : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(RequiredPropertiesInitializationAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (LanguageFeaturesAvailability.ImplicitObjectCreation)
            {
                var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
                if (root.FindNode(context.Span).FirstAncestorOrSelf<BaseObjectCreationExpressionSyntax>() is { } objectCreation)
                {
                    context.RegisterCodeFix(CodeAction.Create("Initialize missing members with default", cancellationToken => CompleteTheInitializationBlock(context, objectCreation, cancellationToken)), context.Diagnostics.First());
                }
            }
        }

        private async Task<Document> CompleteTheInitializationBlock(CodeFixContext context, BaseObjectCreationExpressionSyntax objectCreation, CancellationToken cancellationToken)
        {
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);
            if (semanticModel.GetTypeInfo(objectCreation) is { Type: { } typeSymbol })
            {
                var membersForInitialization = RequiredPropertiesInitializationAnalyzer.GetMembersForRequiredInit(typeSymbol, objectCreation, semanticModel).Select(x => x.Name).ToImmutableHashSet();
                var alreadyInitializedMembers = RequiredPropertiesInitializationAnalyzer.GetAlreadyInitializedMembers(objectCreation.Initializer);
                var missingMembers = membersForInitialization.Except(alreadyInitializedMembers);
                
                var missingAssignments = missingMembers.OrderBy(x=>x).Select(x => SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName(x), SyntaxFactory.IdentifierName("default")));

                if (objectCreation.Initializer is {} initializer)
                {
                    var completeAssignmentsList = initializer.Expressions.Select((x, i)=> i == initializer.Expressions.Count -1 ? x.WithoutTrailingTrivia(): x).Concat(missingAssignments);
                    var newInitializationBlock = initializer.WithExpressions(SyntaxFactory.SeparatedList(completeAssignmentsList)).WithAdditionalAnnotations(Formatter.Annotation);
                    return await ReplaceNodes(context.Document, Formatter.Format(initializer, Formatter.Annotation, context.Document.Project.Solution.Workspace), newInitializationBlock, cancellationToken);
                }
                else
                {
                    var newInitializer = SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression, SyntaxFactory.SeparatedList(missingAssignments.OfType<ExpressionSyntax>())).WithAdditionalAnnotations(Formatter.Annotation);
                    var newObjectCreation = objectCreation.WithInitializer((InitializerExpressionSyntax) Formatter.Format(newInitializer, Formatter.Annotation, context.Document.Project.Solution.Workspace)); 
                    return await ReplaceNodes(context.Document, objectCreation, newObjectCreation, cancellationToken);
                }
            }
            return context.Document;
        }

        public static async Task<Document> ReplaceNodes(Document document, SyntaxNode oldNode, SyntaxNode newNode, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            if (root != null)
            {
                var newRoot = root.ReplaceNode(oldNode, newNode);
                return document.WithSyntaxRoot(newRoot);
            }

            return document;
        }
    }
}
