using System;

namespace SmartAnalyzers.CSharpExtensions.Annotations
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class TwinTypeAttribute : Attribute
    {
        public string[] IgnoredMembers { get; set; }
        public TwinTypeAttribute(Type type)
        {
        }
    }
}