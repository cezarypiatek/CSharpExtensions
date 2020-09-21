using System;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace CSharpExtensions.Analyzers
{
    public static class NullableExtensions
    {
        private static INullabilityResolver _nullabilityResolver;

        private static INullabilityResolver NullabilityResolver
        {

            get
            {
                return _nullabilityResolver ??= ReflectionNullabilityResolver.IsAvailable()
                    ? (INullabilityResolver)new ReflectionNullabilityResolver()
                    : new EmptyNullabilityResolver();
            }
        }


        public static bool IsAnnotatedAsNullable(this ITypeSymbol typeSymbol) => NullabilityResolver.IsAnnotatedAsNullable(typeSymbol);


        private interface INullabilityResolver
        {
            bool IsAnnotatedAsNullable(ITypeSymbol type);
        }

        class EmptyNullabilityResolver : INullabilityResolver
        {
            public bool IsAnnotatedAsNullable(ITypeSymbol type) => false;

        }

        class ReflectionNullabilityResolver : INullabilityResolver
        {
            private readonly MethodInfo nullableAnnotation;
            private readonly MethodInfo withoutNullable;

            //NullableAnnotation.Annotated
            private readonly object annotatedValue;
            //NullableAnnotation.None
            private readonly object nonValue;
            private readonly object[] stripValueParameter;

            private static readonly System.Reflection.TypeInfo typeSymbolInfo = typeof(ITypeSymbol).GetTypeInfo();

            public static bool IsAvailable() => typeSymbolInfo.GetDeclaredProperty("NullableAnnotation") != null;

            public ReflectionNullabilityResolver()
            {
                nullableAnnotation = typeSymbolInfo.GetDeclaredProperty("NullableAnnotation")!.GetMethod;
                withoutNullable = typeSymbolInfo.GetDeclaredMethod("WithNullableAnnotation");
                var nullableAnnotations = Enum.GetValues(withoutNullable.GetParameters()[0].ParameterType);
                annotatedValue = nullableAnnotations.GetValue(2);
                nonValue = nullableAnnotations.GetValue(0);
                stripValueParameter = new[] { nonValue };
            }


            public bool IsAnnotatedAsNullable(ITypeSymbol type)
            {
                //var originalResult = type.NullableAnnotation == NullableAnnotation.Annotated;
                return nullableAnnotation.Invoke(type, Array.Empty<object>()).Equals(annotatedValue);
            }
        }
    }
}