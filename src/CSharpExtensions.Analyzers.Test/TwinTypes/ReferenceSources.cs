using System.Reflection;
using Microsoft.CodeAnalysis;
using RoslynTestKit;
using SmartAnalyzers.CSharpExtensions.Annotations;

namespace CSharpExtensions.Analyzers.Test.TwinTypes;

internal static class ReferenceSources
{
    public static MetadataReference[] CSEReferences => new[]
    {
        ReferenceSource.FromType<TwinTypeAttribute>(),
        MetadataReference.CreateFromFile(Assembly.Load("netstandard, Version=2.1.0.0").Location),
        MetadataReference.CreateFromFile(Assembly.Load("System.Runtime, Version=6.0.0.0").Location)
    };
}