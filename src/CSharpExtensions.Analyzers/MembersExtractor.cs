using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                                       property.IsStatic == false &&
                                       property.IsReadOnly == false &&
                                       property.ExplicitInterfaceImplementations.IsEmpty &&
                                       IsSymbolAccessible(property.SetMethod);
                            case IFieldSymbol field:
                                return field.IsReadOnly == false && 
                                       field.IsStatic == false && 
                                       IsSymbolAccessible(field);
                            default:
                                return false;
                        }
                    });
        }

        private static readonly MethodInfo IsSymbolAccessibleWithinMethod = typeof(Compilation).GetRuntimeMethod("IsSymbolAccessibleWithin", new []
        {
            typeof(ISymbol),
            typeof(ISymbol),
            typeof(ITypeSymbol)
        });

        private bool IsSymbolAccessible(ISymbol x)
        {
            if (_contextSymbol.Value == null)
            {
                return true;
            }

            
            if (IsSymbolAccessibleWithinMethod != null)
            {
                //INFO: Invoke via reflection to support VS2017
                return (bool)IsSymbolAccessibleWithinMethod.Invoke(_semanticModel.Compilation, new[] { x, _contextSymbol.Value, null });
            }


            ClassLocation GetClassLocation()
            {
                if (x.ContainingType == _contextSymbol.Value)
                {
                    return ClassLocation.Declared;

                }

                if (GetBaseTypesAndThis(x.ContainingType).Any(t => t == _contextSymbol.Value))
                {
                    return ClassLocation.Derived;
                }

                return ClassLocation.Other;
            }

            bool IsSameAssembly()
            {
                if (x.ContainingAssembly == _contextSymbol.Value.ContainingAssembly)
                {
                    return true;
                }

                return x.ContainingAssembly.GetAttributes().Any(x =>
                    x.AttributeClass.Name == "InternalsVisibleTo" && x.ConstructorArguments[0].Value ==
                    _contextSymbol.Value.ContainingAssembly.Name);
            }

            var sameAssembly = IsSameAssembly();
            var location = GetClassLocation();

            return (x.DeclaredAccessibility, sameAssembly, location) switch
            {
                (Accessibility.Public, _, _) => true,
                (Accessibility.Private, _, ClassLocation.Declared) => true,
                (Accessibility.Private, _, _) => false,
                (Accessibility.Protected, _, ClassLocation.Other) => false,
                (Accessibility.Protected, _, _) => false,
                (Accessibility.Internal, true, _) => true,
                (Accessibility.Internal, false, _) => false,
                (Accessibility.ProtectedOrInternal, false, ClassLocation.Other) => false,
                (Accessibility.ProtectedOrInternal, _,  _) => true,
                (Accessibility.ProtectedAndInternal, true, ClassLocation.Declared) => true,
                (Accessibility.ProtectedAndInternal, true, ClassLocation.Derived) => true,
                (Accessibility.ProtectedAndInternal, _, _) => false,
                (_,_,_) => false
            };
        }


        private enum ClassLocation
        {
            Declared,
            Other,
            Derived
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