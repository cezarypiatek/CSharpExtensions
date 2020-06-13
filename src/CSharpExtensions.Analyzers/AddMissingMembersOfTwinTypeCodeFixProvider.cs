using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace CSharpExtensions.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    public class AddMissingMembersOfTwinTypeCodeFixProvider: CodeFixProvider
    {
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var typeDeclaration = root.FindNode(context.Span).FirstAncestorOrSelf<TypeDeclarationSyntax>();
            
            if (typeDeclaration is {} && ModelExtensions.GetDeclaredSymbol(semanticModel, typeDeclaration) is INamedTypeSymbol namedType)
            {
                var twinTypes = SymbolHelper.GetTwinTypes(namedType).ToDictionary(x=>x.Type.ToDisplayString(), x=>x);
                foreach (var diagnostic in context.Diagnostics)
                {
                    if (diagnostic.Id == TwinTypeAnalyzer.DiagnosticId && diagnostic.Properties.TryGetValue("TwinType", out var twinTypeName))
                    {
                        if (twinTypes.TryGetValue(twinTypeName, out var twinTypeInfo))
                        {
                            context.RegisterCodeFix(CodeAction.Create($"Add missing members from type {twinTypeName}", token =>  AddMissingMembers(context.Document, namedType, typeDeclaration, twinTypeInfo, token)), diagnostic);
                        }
                    }
                }
            }
        }

        private async Task<Document> AddMissingMembers(Document contextDocument, INamedTypeSymbol namedType, TypeDeclarationSyntax typeDeclaration, TwinTypeInfo twinTypeInfo, CancellationToken token)
        {
            var syntaxGenerator = SyntaxGenerator.GetGenerator(contextDocument);
            var newMembers =  CreateMissingMembers(namedType, twinTypeInfo, syntaxGenerator).ToArray();
            var newType = typeDeclaration.AddMembers(newMembers);
            return await ReplaceNodes(contextDocument, typeDeclaration, newType, token);
        }


        public static async Task<Document> ReplaceNodes(Document document, SyntaxNode oldNode, SyntaxNode newNode, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(oldNode, newNode);
            return document.WithSyntaxRoot(newRoot);
        }

        private static IEnumerable<MemberDeclarationSyntax> CreateMissingMembers(INamedTypeSymbol namedType, TwinTypeInfo twinTypeInfo, SyntaxGenerator syntaxGenerator)
        {
            foreach (var missingMember in twinTypeInfo.GetMissingMembersFor(namedType).OrderBy(x=>x.Symbol.Name))
            {
                if (missingMember.Symbol is IPropertySymbol propertySymbol)
                {
                    yield return CreateAutoProperty(syntaxGenerator, propertySymbol.Name, propertySymbol.Type);
                }
                else if (missingMember.Symbol is IFieldSymbol fieldSymbol)
                {
                    yield return CreateAutoProperty(syntaxGenerator, fieldSymbol.Name, fieldSymbol.Type);
                }
            }
        }

        private static PropertyDeclarationSyntax CreateAutoProperty(SyntaxGenerator syntaxGenerator, string name, ITypeSymbol type)
        {
            var newProperty = (PropertyDeclarationSyntax) syntaxGenerator.PropertyDeclaration(name, syntaxGenerator.TypeExpression(type));
            return newProperty.WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(

                new[]
                {
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                }
            ))).WithAdditionalAnnotations(Formatter.Annotation);
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(TwinTypeAnalyzer.DiagnosticId);
    }
}