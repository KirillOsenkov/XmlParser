using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.Language.Xml.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Language.Xml.Editor
{
	[Export(typeof(ICompletionSourceProvider))]
	[ContentType("xml")]
	[Order]
	public class XmlCompletionSourceProvider : ICompletionSourceProvider
	{
		[Import]
		ParserService parserService = null;
		[ImportMany]
		IEnumerable<IXmlCompletionProvider> providers = null;

		public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
		{
			if (providers == null || !providers.Any())
				return null;
			return new CompletionSource(textBuffer, parserService, providers);
		}
	}
}
