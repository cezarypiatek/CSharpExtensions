using System;

namespace SmartAnalyzers.CSharpExtensions.Annotations
{
    /// <summary>
    ///     Enforces explicit initialization of all non-nullable (value and references) members for every type defined inside an assembly that was annotated with this attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class InitRequiredForNotNullAttribute : Attribute
    {

    }
}