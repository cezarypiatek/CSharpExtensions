using System;

namespace SmartAnalyzers.CSharpExtensions.Annotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class InitRequiredAttribute : Attribute { }
}
