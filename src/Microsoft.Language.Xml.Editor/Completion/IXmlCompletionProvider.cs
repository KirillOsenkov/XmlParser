using System;
using System.Collections.Generic;

namespace Microsoft.Language.Xml.Editor
{
	public interface IXmlCompletionProvider
	{
		string Name { get; }
		string DisplayName { get; }

		IEnumerable<string> GetElementCompletions();

		IEnumerable<string> GetAttributeCompletions(IXmlElement element);

		IEnumerable<string> GetAttributeValueCompletions(IXmlElement element, XmlAttributeSyntax att);
	}
}
