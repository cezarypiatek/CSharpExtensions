using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpExtensions.Analyzers
{
    public class MembersExtractor
    {
        private readonly Lazy<INamedTypeSymbol> _contextSymbol;

        public MembersExtractor(SemanticModel semanticModel, SyntaxNode context)
        {
            _contextSymbol = new Lazy<INamedTypeSymbol>(() =>
            {
                var topNode = FindNearestContainer<TypeDeclarationSyntax>(context);
                if (topNode != null)
                {
                    return semanticModel.GetDeclaredSymbol(topNode) as INamedTypeSymbol;
                }
                return null;
            });
        }

        public MembersExtractor(INamedTypeSymbol contextSymbol)
        {
            _contextSymbol = new Lazy<INamedTypeSymbol>(()=> contextSymbol);
        }


        private static SyntaxNode FindNearestContainer<TExpected1>(SyntaxNode tokenParent)
            where TExpected1 : SyntaxNode
        {
            if (tokenParent is TExpected1 t1)
            {
                return t1;
            }


            return tokenParent.Parent == null ? null : FindNearestContainer<TExpected1>(tokenParent.Parent);
        }

        public IEnumerable<ISymbol> GetAllMembersThatCanBeInitialized(ITypeSymbol type)
        {
            return GetAllMembers(type).Where(m =>
                    {
                        switch (m)
                        {
                            case IPropertySymbol property:
                                return property.SetMethod != null &&
                                       property.IsIndexer == false &&
                                       property.IsStatic == false &&
                                       property.IsReadOnly == false &&
                                       property.ExplicitInterfaceImplementations.IsEmpty &&
                                       IsSymbolAccessible(property.SetMethod, type);
                            case IFieldSymbol field:
                                return field.IsReadOnly == false &&
                                       field.IsStatic == false &&
                                       field.IsImplicitlyDeclared == false &&
                                       IsSymbolAccessible(field, type);
                            default:
                                return false;
                        }
                    });
        }

        private static IEnumerable<ISymbol> GetAllMembers(ITypeSymbol type)
        {
            return GetBaseTypesAndThis(type).SelectMany(x => x.GetMembers());
        }
        
        public IEnumerable<ISymbol> GetAllAccessibleMembers(ITypeSymbol type, Func<ISymbol, bool> filter)
        {
            return GetAllMembers(type).Where(filter).Where(m => IsSymbolAccessible(m, type));
        }

        private bool IsSymbolAccessible(ISymbol x, ITypeSymbol via)
        {
            if (_contextSymbol.Value == null)
            {
                return true;
            }

            return (x.DeclaredAccessibility, IsSameAssembly(x), GetClassLocation(x)) switch
            {
                (Accessibility.Public, _, _) => true,
                (Accessibility.Private, _, ClassLocation.Declared) => true,
                (Accessibility.Protected, true, ClassLocation.Declared) => true,
                (Accessibility.Protected, true, ClassLocation.Derived) => InheritFrom(via, _contextSymbol.Value),
                (Accessibility.Internal, true, _) => true,
                (Accessibility.ProtectedOrInternal, true, _) => true,
                (Accessibility.ProtectedOrInternal, false, ClassLocation.Derived) => InheritFrom(via, _contextSymbol.Value),
                (Accessibility.ProtectedAndInternal, true, ClassLocation.Declared) => true,
                (Accessibility.ProtectedAndInternal, true, ClassLocation.Derived) => InheritFrom(via, _contextSymbol.Value),
                (_, _, _) => false
            };
        }

        ClassLocation GetClassLocation(ISymbol x)
        {
            if (x.ContainingType == _contextSymbol.Value)
            {
                return ClassLocation.Declared;
            }

            if (InheritFrom(_contextSymbol.Value, x.ContainingType))
            {
                return ClassLocation.Derived;
            }

            return ClassLocation.Other;
        }

        private readonly Dictionary<(ITypeSymbol, ITypeSymbol), bool> _inheritanceCache = new Dictionary<(ITypeSymbol, ITypeSymbol), bool>();

        private bool InheritFrom(ITypeSymbol type, INamedTypeSymbol from)
        {
            var key = (type, from);
            if (_inheritanceCache.ContainsKey(key) == false)
            {
                _inheritanceCache[key] = GetBaseTypesAndThis(type).Any(t => t.Equals(from));
            }
            return _inheritanceCache[key];
        }

        private readonly Dictionary<(IAssemblySymbol, IAssemblySymbol), bool> _assemblyRelationCache = new Dictionary<(IAssemblySymbol, IAssemblySymbol), bool>();

        bool IsSameAssembly(ISymbol x)
        {
            var key = (x.ContainingAssembly, _contextSymbol.Value.ContainingAssembly);

            if (_assemblyRelationCache.ContainsKey(key) == false)
            {
                if (x.ContainingAssembly.Equals(_contextSymbol.Value.ContainingAssembly))
                {
                    _assemblyRelationCache[key] = true;
                }
                else
                {
                    _assemblyRelationCache[key] = x.ContainingAssembly.GetAttributes()
                        .Any(x => x.AttributeClass.Name == "InternalsVisibleToAttribute" &&
                                  x.ConstructorArguments[0].Value.ToString()
                                      .StartsWith(_contextSymbol.Value.ContainingAssembly.Name)
                        );
                }

            }
            return _assemblyRelationCache[key];
        }

        private enum ClassLocation
        {
            Declared,
            Other,
            Derived
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
    }
}