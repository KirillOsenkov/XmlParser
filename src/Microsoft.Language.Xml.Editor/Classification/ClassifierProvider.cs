using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Language.Xml.Editor
{
    [Export(typeof(IClassifierProvider))]
    [ContentType("xml")]
    public class ClassifierProvider : IClassifierProvider
    {
        private readonly ParserService parserService;
        private IClassificationType[] types;

        [ImportingConstructor]
        public ClassifierProvider(IClassificationTypeRegistryService classificationTypeRegistryService, ParserService parserService)
        {
            types = new IClassificationType[]
            {
                classificationTypeRegistryService.GetClassificationType("text"),
                classificationTypeRegistryService.GetClassificationType(ClassificationTypeNames.XmlAttributeName),
                classificationTypeRegistryService.GetClassificationType(ClassificationTypeNames.XmlAttributeQuotes),
                classificationTypeRegistryService.GetClassificationType(ClassificationTypeNames.XmlAttributeValue),
                classificationTypeRegistryService.GetClassificationType(ClassificationTypeNames.XmlCDataSection),
                classificationTypeRegistryService.GetClassificationType(ClassificationTypeNames.XmlComment),
                classificationTypeRegistryService.GetClassificationType(ClassificationTypeNames.XmlDelimiter),
                classificationTypeRegistryService.GetClassificationType(ClassificationTypeNames.XmlEntityReference),
                classificationTypeRegistryService.GetClassificationType(ClassificationTypeNames.XmlName),
                classificationTypeRegistryService.GetClassificationType(ClassificationTypeNames.XmlProcessingInstruction),
                classificationTypeRegistryService.GetClassificationType(ClassificationTypeNames.XmlText),
            };
            this.parserService = parserService;
        }

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return new Classifier(types, parserService);
        }
    }
}
