using NUnit.Framework;
using RoslynTestKit;

namespace CSharpExtensions.Analyzers.Test.TwinTypes;

public class TwinTypeCodeFixTests 
{
    private static CodeFixTestFixture CreateFixture(CSE003Settings defaultSettings = null)
    {
        defaultSettings ??= new CSE003Settings();
        return RoslynFixtureFactory.Create
        (
            codeFixProvider: new AddMissingMembersOfTwinTypeCodeFixProvider
            {
                DefaultSettings = defaultSettings
            },
            config: new CodeFixTestFixtureConfig
            {
                References = ReferenceSources.CSEReferences,
                AdditionalAnalyzers = new []
                {
                    new TwinTypeAnalyzer
                    {
                        DefaultSettings = defaultSettings
                    }
                }
            }
        );
    }

    [Test]
    public void should_add_missing_properties_and_fields()
    {
        CreateFixture().TestCodeFix(TwinTypeAnalyzerTestsTestCases._005_MissingMembersForFIx, TwinTypeAnalyzerTestsTestCases._005_MissingMembersForFIx_FIXED, TwinTypeAnalyzer.DiagnosticId);
    }

    [Test]
    public void should_add_missing_fields_for_enum()
    {
        CreateFixture().TestCodeFix(TwinTypeAnalyzerTestsTestCases._009_MissingFieldsForEnum, TwinTypeAnalyzerTestsTestCases._009_MissingFieldsForEnum_FIXED, TwinTypeAnalyzer.DiagnosticId);
    }

    [Test]
    public void should_add_correct_fields_order_for_enum_default_value()
    {
        CreateFixture(new CSE003Settings
        {
            IdenticalEnum =  true
        }).TestCodeFix(TwinTypeAnalyzerTestsTestCases._011_WrongFieldsOrderForEnumDefaultValue, TwinTypeAnalyzerTestsTestCases._011_WrongFieldsOrderForEnumDefaultValue_FIXED, TwinTypeAnalyzer.DiagnosticId);
    }

    [Test]
    public void should_report_wrong_fields_order_for_enum_with_wrong_value()
    {
        CreateFixture(new CSE003Settings
        {
            IdenticalEnum =  true
        }).TestCodeFix(TwinTypeAnalyzerTestsTestCases._012_WrongFieldsOrderForEnumWrongValue, TwinTypeAnalyzerTestsTestCases._012_WrongFieldsOrderForEnumWrongValue_FIXED, TwinTypeAnalyzer.DiagnosticId);
    }

    [Test]
    public void should_add_missing_properties_with_prefix()
    {
        CreateFixture(new CSE003Settings
        {
            IdenticalEnum =  true
        }).TestCodeFix(TwinTypeAnalyzerTestsTestCases._007_PropertiesWithPrefixForFix, TwinTypeAnalyzerTestsTestCases._007_PropertiesWithPrefixForFix_FIXED, TwinTypeAnalyzer.DiagnosticId);
    }

    [Test]
    public void should_add_missing_properties_with_prefix_from_second_twin()
    {
        CreateFixture().TestCodeFix(TwinTypeAnalyzerTestsTestCases._008_PropertiesWithPrefixWithTwoTwinsForFix, TwinTypeAnalyzerTestsTestCases._008_PropertiesWithPrefixWithTwoTwinsForFix_FIXED, TwinTypeAnalyzer.DiagnosticId, 1);
    }
}