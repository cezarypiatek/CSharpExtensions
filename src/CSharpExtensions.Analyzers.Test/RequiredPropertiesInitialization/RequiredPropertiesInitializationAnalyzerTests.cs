using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using RoslynTestKit;
using SmartAnalyzers.CSharpExtensions.Annotations;
using static CSharpExtensions.Analyzers.Test.RequiredPropertiesInitialization.RequiredPropertiesInitializationTestCases;

namespace CSharpExtensions.Analyzers.Test.RequiredPropertiesInitialization
{
    public class RequiredPropertiesInitializationAnalyzerTests : AnalyzerTestFixture
    {
        protected override string LanguageName => LanguageNames.CSharp;
        protected override DiagnosticAnalyzer CreateAnalyzer() => new RequiredPropertiesInitializationAnalyzer();

        protected override IReadOnlyCollection<MetadataReference> References => new[]
        {
            ReferenceSource.FromType<InitRequiredAttribute>()
        };

        [Test]
        public void should_report_missing_properties_for_type_marked_with_full_init_required_attribute_when_empty_init_block()
        {
            this.HasDiagnostic(_001_MissingPropertiesFullInitRequiredAttribute, RequiredPropertiesInitializationAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_report_missing_properties_for_type_marked_with_full_init_required_attribute_when_empty_init_block_inherited_properties()
        {
            this.HasDiagnostic(_002_MissingPropertiesFullInitRequiredAttributeInheritedy, RequiredPropertiesInitializationAnalyzer.DiagnosticId);

        }

        [Test]
        public void should_report_missing_properties_for_type_marked_with_full_init_required_attribute_when_no_init_block()
        {
            this.HasDiagnostic(_003_MissingPropertiesFullInitRequiredAttributeNoInitBlock, RequiredPropertiesInitializationAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_report_missing_properties_for_type_marked_with_full_init_required_comment_when_empty_init_block()
        {
            this.HasDiagnostic(_004_MissingPropertiesFullInitRequiredComment, RequiredPropertiesInitializationAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_report_missing_properties_for_type_marked_with_full_init_required_comment_when_no_init_block()
        {
            this.HasDiagnostic(_005_MissingPropertiesFullInitRequiredCommentNoInitBlock, RequiredPropertiesInitializationAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_report_missing_properties_for_type_marked_with_full_init_required_comment_recursive_options()
        {
            this.HasDiagnostic(_006_MissingPropertiesFullInitRequiredCommentRecursive, RequiredPropertiesInitializationAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_report_missing_properties_marked_with_init_required_()
        {
            this.HasDiagnostic(_007_MissingPropertiesMarkedWithInitRequiredAttribute, RequiredPropertiesInitializationAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_not_report_missing_properties_when_all_assigned_in_init_block()
        {
            this.NoDiagnosticAtLine(_008_AllPropertiesAssigned, RequiredPropertiesInitializationAnalyzer.DiagnosticId, 12);
        }

        [Test]
        public void should_not_report_explicitly_implemented_properties_as_missing()
        {
            this.NoDiagnosticAtLine(_009_DoNotReportMissingPropertiesImplementedExplicitly, RequiredPropertiesInitializationAnalyzer.DiagnosticId, 12);
        }

        [Test]
        public void should_report_missing_properties_for_init_without_new_keyword_when_recursive_init_required()
        {
            this.HasDiagnostic(_010_MissingPropertiesFullInitRequiredCommentRecursiveWhenNoNewKeyword, RequiredPropertiesInitializationAnalyzer.DiagnosticId);
        }

        [Test]
        public void should_not_report_not_accessible_properties_as_missing()
        {
            this.NoDiagnosticAtLine(_011_MissingNotAccessibleProperties, RequiredPropertiesInitializationAnalyzer.DiagnosticId, 12);
        }


        [Test]
        public void should_report_missing_fields_for_type_marked_with_full_init_required_attribute_when_empty_init_block()
        {
            this.HasDiagnostic(_012_MissingFieldsFullInitRequiredAttribute, RequiredPropertiesInitializationAnalyzer.DiagnosticId);
        }
        
        [Test]
        public void should_not_report_missing_init_for_init_only_optional()
        {
            this.NoDiagnostic(_013_DoNotReportMissingPropertiesForInitOnlyOptional, RequiredPropertiesInitializationAnalyzer.DiagnosticId);
        }
    }
}
