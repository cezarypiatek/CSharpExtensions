using System;

namespace SmartAnalyzers.CSharpExtensions.Annotations
{
    /// <summary>
    /// Specifies that the member this attribute is bound to
    /// is readonly and should have default value which could be optionally override in initialized through initialization block.
    /// Applied on the type level, enforces this rule for all members (fields and properties)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class InitOnlyOptionalAttribute : Attribute { }
}