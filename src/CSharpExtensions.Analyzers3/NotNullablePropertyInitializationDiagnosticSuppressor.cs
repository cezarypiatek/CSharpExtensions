using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpExtensions.Analyzers3
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public partial class NotNullablePropertyInitializationDiagnosticSuppressor: DiagnosticSuppressor
    {
        private static readonly SuppressionDescriptor SuppressionDescriptor = new SuppressionDescriptor("SPCS8618", "CS8618", "Member decorated with attribute guaranteeing initialization via init block");

        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = ImmutableArray.Create(SuppressionDescriptor);

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            foreach (var diagnostic in context.ReportedDiagnostics)
            {
                var root = diagnostic.Location.SourceTree.GetRoot().FindNode(diagnostic.Location.SourceSpan);
                if (root is PropertyDeclarationSyntax propertyDeclaration)
                {
                    if (HasAttributeWithInitGuarantee(propertyDeclaration.AttributeLists))
                    {
                        context.ReportSuppression(Suppression.Create(SuppressionDescriptor, diagnostic));
                    }
                    else
                    {
                       var typeDeclaration =  SyntaxHelper.FindNearestContainer<TypeDeclarationSyntax>(propertyDeclaration);
                       if (typeDeclaration != null && HasAttributeWithInitGuarantee(typeDeclaration.AttributeLists))
                       {
                           context.ReportSuppression(Suppression.Create(SuppressionDescriptor, diagnostic));
                       }
                    }
                }
            }
        }

        private bool HasAttributeWithInitGuarantee(SyntaxList<AttributeListSyntax> attributes)
        {
            if (attributes.Count == 0)
            {
                return false;
            }

            foreach (var attribute in attributes.SelectMany(x => x.Attributes))
            {
                var attributeName = attribute.Name.ToFullString().Replace("Attribute","");
                if (attributeName.EndsWith("InitOnly") || attributeName.EndsWith("InitRequired"))
                {
                    return true;
                }
            }

            return true;
        }
    }
}
