using System;
using System.Collections.Generic;

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
        public static string GetContentValue(this IXmlElementSyntax element)
        {
            if (element.Content.Count == 1 && element.Content.First() is XmlCDataSectionSyntax cdata)
                return cdata.TextTokens.ToFullString();
            return element.AsElement.Value;
        }

        /// <summary>
        /// Return a new <see cref="IXmlElementSyntax"/> instance with
        /// the supplied string prefix.
        /// </summary>
        public static IXmlElementSyntax WithPrefixName(this IXmlElementSyntax element, string prefixName)
        {
            var existingName = element.NameNode;
            var existingPrefix = existingName.PrefixNode;
            var newName = SyntaxFactory.XmlNameToken(prefixName, null, null);

            return element.WithName(existingName.WithPrefix(existingPrefix.WithName(newName)));
        }

        /// <summary>
        /// Return a new <see cref="XmlAttributeSyntax"/> instance with
        /// the supplied string attribute value
        /// </summary>
        public static XmlAttributeSyntax WithValue(this XmlAttributeSyntax attribute, string attributeValue)
        {
            var textTokens = SyntaxFactory.SingletonList(SyntaxFactory.XmlTextLiteralToken(attributeValue, null, null));
            return attribute.WithValue(attribute.ValueNode.WithTextTokens(textTokens));
        }

        public static XmlAttributeSyntax WithPrefixName(this XmlAttributeSyntax attribute, string prefixName)
        {
            var existingName = attribute.NameNode;
            var existingPrefix = existingName.PrefixNode;
            var newName = SyntaxFactory.XmlNameToken(prefixName, null, null);

            return attribute.WithName(existingName.WithPrefix(existingPrefix.WithName(newName)));
        }

        public static XmlAttributeSyntax WithLocalName(this XmlAttributeSyntax attribute, string localName)
        {
            var existingName = attribute.NameNode;
            var existingLocalName = existingName.LocalNameNode;
            var newName = SyntaxFactory.XmlNameToken(localName, null, null);

            return attribute.WithName(existingName.WithLocalName(newName));
        }

        public static IXmlElementSyntax AddChild(this IXmlElementSyntax parent, IXmlElementSyntax child)
        {
            return parent.WithContent(parent.Content.Add(child.AsNode));
        }

        public static IXmlElementSyntax InsertChild(this IXmlElementSyntax parent, IXmlElementSyntax child, int index)
        {
            if (index == -1)
                return AddChild(parent, child);
            return parent.WithContent(parent.Content.Insert(index, child.AsNode));
        }

        public static IXmlElementSyntax RemoveChild(this IXmlElementSyntax parent, IXmlElementSyntax child)
        {
            return parent.WithContent(parent.Content.Remove(child.AsNode));
        }

        internal static bool IsXmlNodeName(this XmlNameSyntax name)
        {
            var p = name.Parent;
            switch (p.Kind)
            {
                case SyntaxKind.XmlElement:
                case SyntaxKind.XmlEmptyElement:
                case SyntaxKind.XmlElementStartTag:
                case SyntaxKind.XmlElementEndTag:
                    return true;
                default: return false;
            }
        }

        public static IXmlElementSyntax AddAttributes(this IXmlElementSyntax self, params XmlAttributeSyntax[] attributes)
        {
            return self.WithAttributes(self.AttributesNode.AddRange(attributes));
        }

        public static IXmlElementSyntax AddAttributes(this IXmlElementSyntax self, IEnumerable<XmlAttributeSyntax> attributes)
        {
            return self.WithAttributes(self.AttributesNode.AddRange(attributes));
        }

        public static IXmlElementSyntax AddAttribute(this IXmlElementSyntax self, XmlAttributeSyntax attribute)
        {
            return self.WithAttributes(self.AttributesNode.Add(attribute));
        }

        public static IXmlElementSyntax RemoveAttribute(this IXmlElementSyntax self, XmlAttributeSyntax attribute)
        {
            return self.WithAttributes(self.AttributesNode.Remove(attribute));
        }
    }
}
