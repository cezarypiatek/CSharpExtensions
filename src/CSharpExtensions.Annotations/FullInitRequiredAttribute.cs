using System;

namespace SmartAnalyzers.CSharpExtensions.Annotations
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class FullInitRequiredAttribute : Attribute { }
}