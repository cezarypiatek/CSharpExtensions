using System;
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
        
        public static IEnumerable<TwinTypeInfo> GetTwinTypes(ITypeSymbol type)
        {
            foreach(var twinAttribute in type.GetAttributes().Where(x => x.AttributeClass.Name == "TwinTypeAttribute"))
            {
                var parameter = twinAttribute.ConstructorArguments.FirstOrDefault();
                if (parameter.Value is INamedTypeSymbol twinType)
                {
                    yield return new TwinTypeInfo()
                    {
                        Type = twinType,
                        IgnoredMembers = GetIgnoredMembers(twinAttribute)
                    };
                }
            }
        }

        private static string[] GetIgnoredMembers(AttributeData twinAttribute)
        {
            var ignoredMembersInfo = twinAttribute.NamedArguments.FirstOrDefault(x => x.Key == "IgnoredMembers");

            if (ignoredMembersInfo.Value is TypedConstant value && value.Kind == TypedConstantKind.Array)
            {
               return value.Values.Select(x => x.Value.ToString()).ToArray();
            }

            return Array.Empty<string>();
        }
    }

    public class TwinTypeInfo
    {
        public INamedTypeSymbol Type { get; set; }
        public string[] IgnoredMembers { get; set; }
    }
}
