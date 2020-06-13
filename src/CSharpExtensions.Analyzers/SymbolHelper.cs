using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        public IReadOnlyList<MemberSymbolInfo> GetMissingMembersFor(INamedTypeSymbol namedType)
        {
            var ownMembers = GetMembers(namedType);
            var twinMembers = GetMembers(this.Type).Where(x=> IgnoredMembers.Contains(x.Symbol.Name) == false).ToList();
            return twinMembers.Except(ownMembers).ToList();
        }

        private static IReadOnlyList<MemberSymbolInfo> GetMembers(ITypeSymbol namedType)
        {
            return MembersExtractor.GetAllMembers(namedType)
                .Where(x => x switch
                {
                    IPropertySymbol property => property.IsStatic == false && property.IsIndexer == false,
                    IFieldSymbol field => field.IsImplicitlyDeclared == false && field.IsStatic == false,
                    _ => false
                })
                .Select(x =>new MemberSymbolInfo(x))
                .ToList();
        }
    }

    public class MemberSymbolInfo
    {
        public ISymbol Symbol { get; }

        public MemberSymbolInfo(ISymbol symbol)
        {
            Symbol = symbol;
        }

        protected bool Equals(MemberSymbolInfo other)
        {
            return Equals(Symbol.Name, other?.Symbol?.Name);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MemberSymbolInfo) obj);
        }

        public override int GetHashCode()
        {
            return Symbol?.Name.GetHashCode() ?? 0;
        }
    }
}
