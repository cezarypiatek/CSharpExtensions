using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            return type.GetAttributes().Any(x => x.AttributeClass.ToDisplayString() == attributeName);
        }

        public static IEnumerable<TwinTypeInfo> GetTwinTypes(ITypeSymbol type, CSE003Settings config)
        {
            foreach (var twinAttribute in type.GetAttributes().Where(x => x.AttributeClass.Name == "TwinTypeAttribute"))
            {
                var parameter = twinAttribute.ConstructorArguments.FirstOrDefault();
                if (parameter.Value is INamedTypeSymbol twinType)
                {
                    yield return new TwinTypeInfo()
                    {
                        Type = twinType,
                        IgnoredMembers = GetIgnoredMembers(twinAttribute),
                        NamePrefix = TryGetNamePrefix(twinAttribute),
                        IdenticalEnum = config.IdenticalEnum
                    };
                }
            }
        }

        private static string TryGetNamePrefix(AttributeData twinAttribute)
        {
            return twinAttribute.NamedArguments.FirstOrDefault(x => x.Key == "NamePrefix").Value.Value?.ToString();
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
        public string NamePrefix { get; set; }
        public bool IdenticalEnum { get; set; }

        public IReadOnlyList<MemberSymbolInfo> GetMissingMembersFor(INamedTypeSymbol namedType)
        {
            var memberExtractor = new MembersExtractor(namedType);

            var ownMembers = GetMembers(memberExtractor, namedType, IdenticalEnum);
            var twinMembers = GetMembers(memberExtractor, this.Type, IdenticalEnum, NamePrefix).Where(x => IgnoredMembers.Contains(x.Symbol.Name) == false).ToList();
            return twinMembers.Except(ownMembers).ToList();
        }

        public IReadOnlyList<MemberSymbolInfo> GetTwinMembersFor(INamedTypeSymbol namedType)
        {
            var memberExtractor = new MembersExtractor(namedType);
            var twinMembers = GetMembers(memberExtractor, this.Type, IdenticalEnum, NamePrefix).Where(x => IgnoredMembers.Contains(x.Symbol.Name) == false).ToList();
            return twinMembers.ToList();
        }

        private static IReadOnlyList<MemberSymbolInfo> GetMembers(MembersExtractor membersExtractor, ITypeSymbol namedType, bool identicalEnum, string namePrefix = null)
        {
            return membersExtractor.GetAllAccessibleMembers(namedType, x => x switch
                {
                    IPropertySymbol property => property.IsStatic == false && property.IsIndexer == false,
                    IFieldSymbol field when namedType.TypeKind == TypeKind.Enum => field.IsImplicitlyDeclared == false,
                    IFieldSymbol field when namedType.TypeKind != TypeKind.Enum => field.IsImplicitlyDeclared == false && field.IsStatic == false,
                    _ => false
                })
                .Select(x => new MemberSymbolInfo(x, identicalEnum, namePrefix))
                .ToList();
        }
    }

    public class MemberSymbolInfo
    {
        public ISymbol Symbol { get; }
        public string ExpectedName { get; }
        public bool IsEnumWithValue { get; }
        public object EnumConstantValue { get; }
        public bool IdenticalEnum { get; }

        public MemberSymbolInfo(ISymbol symbol, bool identicalEnum, string namePrefix)
        {
            Symbol = symbol;
            IdenticalEnum = identicalEnum;
            ExpectedName = namePrefix + symbol.Name;

            if (IdenticalEnum && symbol is IFieldSymbol ff && ff.DeclaringSyntaxReferences[0].GetSyntax() is EnumMemberDeclarationSyntax e)
            {
                IsEnumWithValue = e.EqualsValue != null;
                EnumConstantValue = ff.ConstantValue;
            }
        }

        protected bool Equals(MemberSymbolInfo other)
        {
            // For enums we want to make sure the constant has the same value.
            if (IdenticalEnum && Symbol is IFieldSymbol f && f.ContainingType.TypeKind == TypeKind.Enum)
            {
                return Equals(ExpectedName, other?.ExpectedName) && Equals(EnumConstantValue, other?.EnumConstantValue);
            }
            return Equals(ExpectedName, other?.ExpectedName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MemberSymbolInfo)obj);
        }

        public override int GetHashCode()
        {
            return ExpectedName?.GetHashCode() ?? 0;
        }
    }
}
