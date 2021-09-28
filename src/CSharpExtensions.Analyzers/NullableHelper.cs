using System;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace CSharpExtensions.Analyzers
{
    public static class NullableHelper
    {
        private static readonly PropertyInfo? PropertyNullableAnnotation = typeof(IPropertySymbol).GetRuntimeProperty("NullableAnnotation");
        private static readonly PropertyInfo? FieldNullableAnnotation = typeof(IFieldSymbol).GetRuntimeProperty("NullableAnnotation");

        public static bool IsNotNullable(ISymbol symbol)
        {
            switch (symbol)
            {
                case IPropertySymbol property:
                {
                    if (property.Type.IsValueType)
                    {
                        if (property.Type is { Name: "Nullable", TypeKind: TypeKind.Struct, ContainingNamespace: { Name: "System" } })
                        {
                            return false;
                        }
                        return true;
                        
                    }
                    var value = PropertyNullableAnnotation?.GetValue(property);
                    return value != null && Convert.ToInt32(value) == 1;
                }
                case IFieldSymbol field:
                {
                    if (field.Type.IsValueType)
                    {
                        if (field.Type is { Name: "Nullable", TypeKind: TypeKind.Struct, ContainingNamespace: { Name: "System" } })
                        {
                            return false;
                        }
                        return true;
                    }
                    var value = FieldNullableAnnotation?.GetValue(field);
                    return value != null && Convert.ToInt32(value) == 1;
                }
                default:
                    return false;
            }
        }


        public static bool IsNullableMember(ISymbol symbol) => symbol switch
        {
            IPropertySymbol property => IsNullable(property.Type),
            IFieldSymbol field => IsNullable(field.Type),
            _ => false
        };

        private static bool IsNullable(ITypeSymbol type)
        {
            if (type.IsValueType && type.Name == "Nullable" && type.ContainingNamespace?.Name == "System")
            {
                return true;
            }

            return type.IsAnnotatedAsNullable() == true;
        }
    }
}
