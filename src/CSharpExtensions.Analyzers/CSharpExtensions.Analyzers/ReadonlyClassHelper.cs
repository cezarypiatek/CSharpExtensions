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
    }
}
