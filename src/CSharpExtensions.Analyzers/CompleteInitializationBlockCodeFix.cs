using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace CSharpExtensions.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CompleteInitializationBlockCodeFix)), Shared]
    public class CompleteInitializationBlockCodeFix : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(RequiredPropertiesInitializationAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root.FindNode(context.Span).FirstAncestorOrSelf<ObjectCreationExpressionSyntax>() is { } objectCreation && objectCreation.ArgumentList?.Arguments.Count > 0)
            {
                foreach (var diagnostic in context.Diagnostics)
                {
                    if (diagnostic.Id == RequiredPropertiesInitializationAnalyzer.DiagnosticId)
                    {
                        context.RegisterCodeFix(CodeAction.Create("Try to move initialization from constructor to init block", token => ReplaceConstructorWithInit(objectCreation, context, token)), diagnostic);

                    }
                } 
            }
        }

        private async Task<Document> ReplaceConstructorWithInit(ObjectCreationExpressionSyntax objectCreation, CodeFixContext context, CancellationToken token)
        {
            var semanticModel = await context.Document.GetSemanticModelAsync(token);
            var extraInitializations = new List<ExpressionSyntax>();
            var unmatchedArguments = new List<(string, ArgumentSyntax)>();
            
            
            if (semanticModel.GetTypeInfo(objectCreation) is  { Type: { } typeSymbol } && semanticModel.GetSymbolInfo(objectCreation) is { Symbol: IMethodSymbol constructorSymbol } && objectCreation.ArgumentList is { } argumentList)
            {

                var membersExtractor = new MembersExtractor(semanticModel, objectCreation);
                var membersForInitialization = membersExtractor.GetAllMembersThatCanBeInitialized(typeSymbol).Select(x => x.Name).ToList();
               
                foreach (var argument in argumentList.Arguments)
                {
                    if (GetArgumentName(argument, constructorSymbol) is { } argumentName)
                    {
                        if (membersForInitialization.FirstOrDefault(x => string.Equals(x, argumentName, StringComparison.OrdinalIgnoreCase)) is {} propertyCandidate )
                        {
                            var newInit = AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, IdentifierName(propertyCandidate), argument.Expression);
                            extraInitializations.Add(newInit);
                        }
                        else
                        {
                            unmatchedArguments.Add((argumentName, argument));
                        }
                    }
                }

               
            }
            var namedArgumentsRest = unmatchedArguments.Select(x =>
            {
                var (name, argument) = x;
                return argument.WithNameColon(NameColon(name));
            }).ToList();

            var newObjectCreation = objectCreation;

            if (namedArgumentsRest.Count > 0 && newObjectCreation.ArgumentList != null)
            {
                newObjectCreation = newObjectCreation.WithArgumentList(newObjectCreation.ArgumentList.WithArguments(SeparatedList(namedArgumentsRest)));
            }
            else
            {
                newObjectCreation = newObjectCreation.WithArgumentList(null);
            }

            if (newObjectCreation.Initializer != null)
            {
                var newSetOfInitExpressions = newObjectCreation.Initializer.Expressions.AddRange(extraInitializations);
                var newInitializer = newObjectCreation.Initializer.WithExpressions(newSetOfInitExpressions);
                newObjectCreation = newObjectCreation.WithInitializer(FixInitializerExpressionFormatting(newInitializer, newObjectCreation));
            }
            else
            {
                var newInitializer = SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression, SeparatedList(extraInitializations));
                newObjectCreation = newObjectCreation.WithInitializer(FixInitializerExpressionFormatting(newInitializer, newObjectCreation));
            }
            
            return await ReplaceNodes(context.Document, objectCreation, newObjectCreation.WithAdditionalAnnotations(Formatter.Annotation), token);
        }

        public static InitializerExpressionSyntax FixInitializerExpressionFormatting(InitializerExpressionSyntax initializerExpressionSyntax, ObjectCreationExpressionSyntax objectCreationExpression)
        {
            var trivia = objectCreationExpression.ArgumentList?.CloseParenToken.TrailingTrivia ?? objectCreationExpression.Type.GetTrailingTrivia();
            if (trivia.ToFullString().Contains(Environment.NewLine))
            {
                return initializerExpressionSyntax;
            }
            return initializerExpressionSyntax
                .WithLeadingTrivia(SyntaxTriviaList.Create(SyntaxFactory.EndOfLine(Environment.NewLine)));
        }

        public static async Task<Document> ReplaceNodes(Document document, SyntaxNode oldNode, SyntaxNode newNode, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(oldNode, newNode);
            return document.WithSyntaxRoot(newRoot);
        }

        private static string GetArgumentName(ArgumentSyntax argument, IMethodSymbol constructorSymbol)
        {
            if (argument.NameColon is {Name: {Identifier: {Text: var argumentName}}} && string.IsNullOrWhiteSpace(argumentName))
            {
                return argumentName;
            }

            if(argument.Parent is ArgumentListSyntax argumentList)
            {
                var index = argumentList.Arguments.IndexOf(argument);
                if (index < 0)
                {
                    return null;
                }

                if (index < constructorSymbol.Parameters.Length)
                {
                    return constructorSymbol.Parameters[index].Name;
                }

                if (index >= constructorSymbol.Parameters.Length &&
                    constructorSymbol.Parameters[constructorSymbol.Parameters.Length - 1].IsParams)
                {
                    return constructorSymbol.Parameters[constructorSymbol.Parameters.Length - 1].Name;
                }
            }

            return null;
        }
    }
}
