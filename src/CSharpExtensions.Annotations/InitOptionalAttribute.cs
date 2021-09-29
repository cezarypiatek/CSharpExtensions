using System;

namespace SmartAnalyzers.CSharpExtensions.Annotations
{
    /// <summary>
    /// Specifies that the member this attribute is bound to
    /// is excluded from the mandatory initialization imposed by <see cref="InitRequiredAttribute"/> or <see cref="InitRequiredForNotNullAttribute"/>
    /// Applied on the type level, enforces this rule for all members (fields and properties)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class InitOptionalAttribute : Attribute { }
}