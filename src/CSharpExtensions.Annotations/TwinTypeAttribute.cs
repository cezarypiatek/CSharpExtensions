using System;

namespace SmartAnalyzers.CSharpExtensions.Annotations
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class TwinTypeAttribute : Attribute
    {
        public string[] IgnoredMembers { get; set; }
        public TwinTypeAttribute(Type type)
        {
        }
    }
}