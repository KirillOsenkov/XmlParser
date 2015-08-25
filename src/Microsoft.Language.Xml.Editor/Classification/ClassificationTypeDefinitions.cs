using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Language.Xml
{
    public class ClassificationTypeDefinitions
    {
        #region VB XML Literals - Attribute Name
        [Export]
        [Name(ClassificationTypeNames.XmlAttributeName)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralAttributeNameTypeDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlAttributeName)]
        [Name(ClassificationTypeNames.XmlAttributeName)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralAttributeNameFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralAttributeNameFormatDefinition()
            {
                this.DisplayName = "XML Attribute";
                this.ForegroundColor = Colors.Red; // HC_LIGHTRED
            }
        }
        #endregion

        #region VB XML Literals - Attribute Quotes
        [Export]
        [Name(ClassificationTypeNames.XmlAttributeQuotes)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralAttributeQuotesTypeDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlAttributeQuotes)]
        [Name(ClassificationTypeNames.XmlAttributeQuotes)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralAttributeQuotesFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralAttributeQuotesFormatDefinition()
            {
                this.DisplayName = "XML Attribute Quotes";
                this.ForegroundColor = Colors.Black; // HC_LIGHTBLACK
            }
        }
        #endregion

        #region VB XML Literals - Attribute Value
        [Export]
        [Name(ClassificationTypeNames.XmlAttributeValue)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralAttributeValueTypeDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlAttributeValue)]
        [Name(ClassificationTypeNames.XmlAttributeValue)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralAttributeValueFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralAttributeValueFormatDefinition()
            {
                this.DisplayName = "XML Attribute Value";
                this.ForegroundColor = Colors.Blue; // HC_LIGHTBLUE
            }
        }
        #endregion

        #region VB XML Literals - CData Section
        [Export]
        [Name(ClassificationTypeNames.XmlCDataSection)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralCDataSectionTypeDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlCDataSection)]
        [Name(ClassificationTypeNames.XmlCDataSection)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralCDataSectionFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralCDataSectionFormatDefinition()
            {
                this.DisplayName = "XML CData Section";
                this.ForegroundColor = Colors.Gray; // HC_LIGHTGRAY
            }
        }
        #endregion

        #region VB XML Literals - Comment
        [Export]
        [Name(ClassificationTypeNames.XmlComment)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralCommentTypeDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlComment)]
        [Name(ClassificationTypeNames.XmlComment)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralCommentFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralCommentFormatDefinition()
            {
                this.DisplayName = "XML Comment";
                this.ForegroundColor = Colors.Green; // HC_LIGHTGREEN
            }
        }
        #endregion

        #region VB XML Literals - Delimiter
        [Export]
        [Name(ClassificationTypeNames.XmlDelimiter)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralDelimiterTypeDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDelimiter)]
        [Name(ClassificationTypeNames.XmlDelimiter)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralDelimiterFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralDelimiterFormatDefinition()
            {
                this.DisplayName = "XML Delimiter";
                this.ForegroundColor = Colors.Blue; // HC_LIGHTBLUE
            }
        }
        #endregion

        #region VB XML Literals - Entity Reference
        [Export]
        [Name(ClassificationTypeNames.XmlEntityReference)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralEntityReferenceTypeDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlEntityReference)]
        [Name(ClassificationTypeNames.XmlEntityReference)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralEntityReferenceFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralEntityReferenceFormatDefinition()
            {
                this.DisplayName = "XML Entity Reference";
                this.ForegroundColor = Colors.Red; // HC_LIGHTRED
            }
        }
        #endregion

        #region VB XML Literals - Name
        [Export]
        [Name(ClassificationTypeNames.XmlName)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralNameTypeDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlName)]
        [Name(ClassificationTypeNames.XmlName)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralNameFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralNameFormatDefinition()
            {
                this.DisplayName = "XML Name";
                this.ForegroundColor = Color.FromRgb(163, 21, 21); // HC_LIGHTMAROON
            }
        }
        #endregion

        #region VB XML Literals - Processing Instruction
        [Export]
        [Name(ClassificationTypeNames.XmlProcessingInstruction)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralProcessingInstructionTypeDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlProcessingInstruction)]
        [Name(ClassificationTypeNames.XmlProcessingInstruction)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralProcessingInstructionFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralProcessingInstructionFormatDefinition()
            {
                this.DisplayName = "XML Processing Instruction";
                this.ForegroundColor = Colors.Gray; // HC_LIGHTGRAY
            }
        }
        #endregion

        #region VB XML Literals - Text
        [Export]
        [Name(ClassificationTypeNames.XmlText)]
        [BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
        internal readonly ClassificationTypeDefinition XmlLiteralTextTypeDefinition = null;

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlText)]
        [Name(ClassificationTypeNames.XmlText)]
        [Order(After = LanguagePriority.NaturalLanguage, Before = LanguagePriority.FormalLanguage)]
        [UserVisible(true)]
        private class XmlLiteralTextFormatDefinition : ClassificationFormatDefinition
        {
            private XmlLiteralTextFormatDefinition()
            {
                this.DisplayName = "XML Text";
                this.ForegroundColor = Colors.Black; // HC_LIGHTBLACK
            }
        }
        #endregion
    }
}
