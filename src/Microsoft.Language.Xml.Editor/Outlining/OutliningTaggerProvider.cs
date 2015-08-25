using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Language.Xml.Editor
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType(ContentType.Xml)]
    public class OutliningTaggerProvider : ITaggerProvider
    {
        [Import]
        public ParserService ParserService { get; set; }

        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            Func<ITagger<T>> factory = () => new OutliningTagger(this, buffer) as ITagger<T>;
            return buffer.Properties.GetOrCreateSingletonProperty(factory);
        }
    }
}
