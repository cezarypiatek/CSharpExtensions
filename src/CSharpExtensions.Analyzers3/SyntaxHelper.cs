using Microsoft.CodeAnalysis;

namespace CSharpExtensions.Analyzers3
{
    internal static class SyntaxHelper
    {
        public static TExpected FindNearestContainer<TExpected>(SyntaxNode tokenParent) where TExpected : SyntaxNode
        {
            if (tokenParent == null)
            {
                return null;
            }

            if (tokenParent is TExpected t1)
            {
                return t1;
            }
            return FindNearestContainer<TExpected>(tokenParent.Parent);
        }
    }
}
