using System;

namespace Microsoft.Language.Xml
{
	public static class XmlExtensions
	{
		/// <summary>
		/// Returns the text content of a element node
		/// </summary>
		/// <remarks>
		/// In addition to the straightforward case
		/// of an element containing simple text tokens, this
		/// method also check for embedded CDATA sections
		/// </remarks>
		public static string GetContentValue (this IXmlElementSyntax element)
		{
			if (element.Content.Count == 1 && element.Content.First () is XmlCDataSectionSyntax cdata)
				return cdata.TextTokens.ToFullString ();
			return element.AsElement.Value;
		}

		/// <summary>
		/// Return a new <see cref="XmlAttributeSyntax"/> instance with
		/// the supplied string attribute value
		/// </summary>
		public static XmlAttributeSyntax WithValue (this XmlAttributeSyntax attribute, string attributeValue)
		{
			var textTokens = SyntaxFactory.SingletonList (SyntaxFactory.XmlTextLiteralToken (attributeValue, null, null));
			return attribute.WithValue (attribute.ValueNode.WithTextTokens (textTokens));
		}

		public static IXmlElementSyntax AddChild (this IXmlElementSyntax parent, IXmlElementSyntax child)
		{
			return parent.WithContent (parent.Content.Add (child.AsNode));
		}

		public static IXmlElementSyntax InsertChild (this IXmlElementSyntax parent, IXmlElementSyntax child, int index)
		{
			if (index == -1)
				return AddChild (parent, child);
			return parent.WithContent (parent.Content.Insert (index, child.AsNode));
		}

		public static IXmlElementSyntax RemoveChild (this IXmlElementSyntax parent, IXmlElementSyntax child)
		{
			return parent.WithContent (parent.Content.Remove (child.AsNode));
		}

        internal static bool IsXmlNodeName (this XmlNameSyntax name)
		{
			var p = name.Parent;
			switch (p.Kind) {
			case SyntaxKind.XmlElement:
			case SyntaxKind.XmlEmptyElement:
			case SyntaxKind.XmlElementStartTag:
			case SyntaxKind.XmlElementEndTag:
				return true;
			default: return false;
			}
		}
	}
}
