using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpExtensions.Analyzers
{
    class MembersExtractor
    {
        private readonly SemanticModel _semanticModel;
        private readonly Lazy<ISymbol> _contextSymbol;

        public MembersExtractor(SemanticModel semanticModel, SyntaxNode context)
        {
            _semanticModel = semanticModel;
            _contextSymbol = new Lazy<ISymbol>(() =>
            {
                var topNode = FindNearestContainer<TypeDeclarationSyntax>(context);
                if (topNode != null)
                {
                    return semanticModel.GetDeclaredSymbol(topNode) as INamedTypeSymbol;
                }
                return null;
            });
        }


        private static SyntaxNode FindNearestContainer<TExpected1>(SyntaxNode tokenParent) 
            where TExpected1 : SyntaxNode 
        {
            if (tokenParent is TExpected1 t1)
            {
                return t1;
            }


            return tokenParent.Parent == null? null: FindNearestContainer<TExpected1>(tokenParent.Parent);
        }

        public IEnumerable<ISymbol> GetAllMembersThatCanBeInitialized(ITypeSymbol type)
        {
            return GetBaseTypesAndThis(type).SelectMany(x => x.GetMembers()).Where(m =>
                    {
                        switch (m)
                        {
                            case IPropertySymbol property:
                                return property.SetMethod != null &&
                                       property.IsIndexer == false &&
                                       property.ExplicitInterfaceImplementations.IsEmpty &&
                                       IsSymbolAccessible(property.SetMethod);
                            case IFieldSymbol field:
                                return IsSymbolAccessible(field);
                            default:
                                return false;
                        }
                    });
        }

        private bool IsSymbolAccessible(ISymbol x)
        {
            return _contextSymbol.Value == null || _semanticModel.Compilation.IsSymbolAccessibleWithin(x, _contextSymbol.Value);
        }

        private IEnumerable<ITypeSymbol> GetBaseTypesAndThis(ITypeSymbol type)
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
        private IEnumerable<ITypeSymbol> UnwrapGeneric(ITypeSymbol typeSymbol)
        {
            if (typeSymbol.TypeKind == TypeKind.TypeParameter && typeSymbol is ITypeParameterSymbol namedType && namedType.Kind != SymbolKind.ErrorType)
            {
                return namedType.ConstraintTypes;
            }
            return new[] { typeSymbol };
        }

        private bool IsSystemObject(ITypeSymbol current)
        {
            return current.Name == "Object" && current.ContainingNamespace.Name == "System";
        }
    }
}