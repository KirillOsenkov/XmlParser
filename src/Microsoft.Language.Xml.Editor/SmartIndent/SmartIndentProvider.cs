using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Language.Xml.Editor
{
    [Export(typeof(ISmartIndentProvider))]
    [ContentType(ContentType.Xml)]
    public class SmartIndentProvider : ISmartIndentProvider
    {
        [Import]
        private ParserService parserService = null;

        public ISmartIndent CreateSmartIndent(ITextView textView)
        {
            return new SmartIndent(textView, parserService);
        }
    }
}
