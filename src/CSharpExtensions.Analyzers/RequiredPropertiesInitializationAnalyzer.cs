using System;
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
                    var annotatedParent = FindNearestContainer<ObjectCreationExpressionSyntax, MethodDeclarationSyntax>(objectCreation.Parent, node => IsMarkedWithComment(node, "FullInitRequired:recursive"));
                    if (annotatedParent is null)
                    {
                        return;
                    }

                    membersForInitialization = GetAllMembersThatCanBeInitialized(type).Select(x => x.Name).ToImmutableHashSet();
                    if (membersForInitialization.IsEmpty)
                    {
                        return;
                    }
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

        public static TExpected FindNearestContainer<TExpected, TStop>(SyntaxNode tokenParent, Func<TExpected, bool> test) where TExpected : SyntaxNode where TStop : SyntaxNode
        {
            if (tokenParent is TExpected t1 && test(t1))
            {
                return t1;
            }

            if (tokenParent is TStop || tokenParent.Parent == null)
            {
                return null;
            }
            
            return FindNearestContainer<TExpected, TStop>(tokenParent.Parent, test);
        }

        private static IEnumerable<ISymbol> GetMembersForRequiredInitialization(ITypeSymbol type, ObjectCreationExpressionSyntax objectCreation)
        {
            var members = GetAllMembersThatCanBeInitialized(type);
            if (IsFullInitRequired(type, objectCreation))
            {
                return members;
            }
            return members.Where(x=> ReadonlyClassHelper.IsMarkedWithAttribute(x, "InitRequiredAttribute"));
        }

        private static IEnumerable<ISymbol> GetAllMembersThatCanBeInitialized(ITypeSymbol type)
        {
            
            return GetBaseTypesAndThis(type).SelectMany(x=> x.GetMembers()).Where(x => x is IPropertySymbol property && 
                                                property.SetMethod != null && 
                                                property.IsIndexer == false && 
                                                property.ExplicitInterfaceImplementations.IsEmpty);
        }

        private static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(ITypeSymbol type)
        {
            foreach (var unwrapped in UnwrapGeneric(type))
            {
                var current = unwrapped;
                while (current != null && IsSystemObject(current) == false)
                {
                    yield return current;
                    current = current.BaseType;
                }
            }
        }
        private static IEnumerable<ITypeSymbol> UnwrapGeneric(ITypeSymbol typeSymbol)
        {
            if (typeSymbol.TypeKind == TypeKind.TypeParameter && typeSymbol is ITypeParameterSymbol namedType && namedType.Kind != SymbolKind.ErrorType)
            {
                return namedType.ConstraintTypes;
            }
            return new[] { typeSymbol };
        }

        private static bool IsSystemObject(ITypeSymbol current)
        {
            return current.Name == "Object" && current.ContainingNamespace.Name == "System";
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