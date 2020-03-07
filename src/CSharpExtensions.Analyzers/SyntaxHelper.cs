using System;
using Microsoft.CodeAnalysis;

namespace CSharpExtensions.Analyzers
{
    public class SyntaxHelper
    {
        public static TExpected FindNearestContainer<TExpected, TStop>(SyntaxNode tokenParent, Func<TExpected, bool> test) where TExpected : SyntaxNode where TStop : SyntaxNode
        {
            if (tokenParent is TExpected t1 && test(t1))
            {
                return t1;
            }

            if (tokenParent is TStop || tokenParent.Parent == null)
            {
                return null;
            }
            
            return FindNearestContainer<TExpected, TStop>(tokenParent.Parent, test);
        }
        
        public static TExpected FindNearestContainer<TExpected>(SyntaxNode tokenParent) where TExpected : SyntaxNode 
        {
            if (tokenParent is TExpected t1)
            {
                return t1;
            }
            return FindNearestContainer<TExpected>(tokenParent.Parent);
        }
    }
}