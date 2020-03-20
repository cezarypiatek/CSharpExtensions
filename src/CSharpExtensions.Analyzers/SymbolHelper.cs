using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace CSharpExtensions.Analyzers
{
    public class SymbolHelperCache
    {
        private readonly Dictionary<ISymbol, Dictionary<string, bool>> _symbolAttributes = new Dictionary<ISymbol, Dictionary<string, bool>>();

        public bool IsMarkedWithAttribute(ISymbol type, string attributeName)
        {
            if (_symbolAttributes.ContainsKey(type) == false)
            {
                _symbolAttributes.Add(type, new Dictionary<string, bool>());
            }

            var bucket = _symbolAttributes[type];
            if (bucket.ContainsKey(attributeName) == false)
            {
                bucket[attributeName] = SymbolHelper.IsMarkedWithAttribute(@type, attributeName);
            }

            return bucket[attributeName];
        }
    }

    public class SymbolHelper
    {
        public static bool IsMarkedWithAttribute(ISymbol type, string attributeName)
        {
            return type.GetAttributes().Any(x =>  x.AttributeClass.ToDisplayString() == attributeName);
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
