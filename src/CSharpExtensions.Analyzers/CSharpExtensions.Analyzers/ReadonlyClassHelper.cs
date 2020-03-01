using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace CSharpExtensions.Analyzers
{
    public static class ReadonlyClassHelper
    {
        public static bool IsMarkedAsReadonly(ITypeSymbol type)
        {
            return type.GetAttributes().Any(x => x.AttributeClass.Name == "ReadonlyAttribute");
        }
        
        public static IEnumerable<ITypeSymbol> GetTwinTypes(ITypeSymbol type)
        {
            foreach(var twinAttribute in type.GetAttributes().Where(x => x.AttributeClass.Name == "TwinTypeAttribute"))
            {
                var parameter = twinAttribute.ConstructorArguments.FirstOrDefault();
                if (parameter.Value is INamedTypeSymbol twinType)
                {
                    yield return twinType;
                }
            }
        }
    }
}
