using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Newtonsoft.Json.Linq;

namespace CSharpExtensions.Analyzers
{
    public static class ConfigReader
    {
        private const string CsharpExtensionsJson = "CSharpExtensions.json";

        public static T GetConfigFor<T>(this AnalyzerOptions options, string diagnosticId, CancellationToken cancellationToken) where T : new()
        {
            var configFile = options.AdditionalFiles.FirstOrDefault(x => Path.GetFileName(x.Path).Equals(CsharpExtensionsJson, StringComparison.OrdinalIgnoreCase));

            if (configFile != null)
            {
                var configPayload = configFile.GetText(cancellationToken).ToString();
                var jObject = JObject.Parse(configPayload);
                if (jObject.TryGetValue(diagnosticId, out var diagnosticConfig))
                {
                    return diagnosticConfig.ToObject<T>();
                }
            }
            return new T();
        }

        public static async Task<T> GetConfigFor<T>(this Project options, string diagnosticId, CancellationToken cancellationToken) where T : new()
        {
            var configFile = options.AdditionalDocuments.FirstOrDefault(x => Path.GetFileName(x.FilePath).Equals(CsharpExtensionsJson, StringComparison.OrdinalIgnoreCase));

            if (configFile != null)
            {
                var configPayload = (await configFile.GetTextAsync(cancellationToken)).ToString();
                var jObject = JObject.Parse(configPayload);
                if (jObject.TryGetValue(diagnosticId, out var diagnosticConfig))
                {
                    return diagnosticConfig.ToObject<T>();
                }
            }
            return new T();
        }
    }
}