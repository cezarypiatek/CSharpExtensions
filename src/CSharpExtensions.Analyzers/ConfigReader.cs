using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Newtonsoft.Json.Linq;

namespace CSharpExtensions.Analyzers
{
    public static class ConfigReader
    {
        public static T GetConfigFor<T>(this AnalyzerOptions options, string diagnosticId, CancellationToken cancellationToken) where T : new()
        {
            var configFile =options.AdditionalFiles.FirstOrDefault(x => Path.GetFileName(x.Path).Equals("CSharpExtensions.json", StringComparison.OrdinalIgnoreCase));
            
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
    }
}