using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpExtensions.Analyzers
{
    public static class LanguageFeaturesAvailability
    {
        public static bool ImplicitObjectCreation { get; set; } = CheckImplicitObjectCreationAvailability();

        private static bool CheckImplicitObjectCreationAvailability()
        {
            return typeof(ObjectCreationExpressionSyntax).GetTypeInfo().BaseType.Name == "BaseObjectCreationExpressionSyntax";
        }
    }
}