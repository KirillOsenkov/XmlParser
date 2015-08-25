using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Language.Xml
{
    public class XmlEmptyElementSyntax : XmlNodeSyntax, IXmlElement
    {
        public PunctuationSyntax LessThanToken;
        public XmlNameSyntax NameNode;
        public SyntaxNode AttributesNode;
        public PunctuationSyntax SlashGreaterThanToken;

        public XmlEmptyElementSyntax(
            PunctuationSyntax lessThanToken,
            XmlNameSyntax name,
            SyntaxNode attributes,
            PunctuationSyntax slashGreaterThanToken) : base(SyntaxKind.XmlEmptyElement)
        {
            this.LessThanToken = lessThanToken;
            this.NameNode = name;
            this.AttributesNode = attributes;
            this.SlashGreaterThanToken = slashGreaterThanToken;
            this.SlotCount = 4;
        }

        IXmlElement IXmlElement.Parent
        {
            get
            {
                var current = this.Parent;
                while (current != null)
                {
                    if (current is IXmlElement)
                    {
                        return current as IXmlElement;
                    }

                    current = current.Parent;
                }

                return null;
            }
        }

        public override SyntaxNode GetSlot(int index)
        {
            switch (index)
            {
                case 0: return LessThanToken;
                case 1: return NameNode;
                case 2: return AttributesNode;
                case 3: return SlashGreaterThanToken;
                default:
                    throw new InvalidOperationException();
            }
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlEmptyElement(this);
        }

        public string Name
        {
            get
            {
                if (NameNode == null)
                {
                    return null;
                }

                return NameNode.Name;
            }
        }

        public IEnumerable<IXmlElement> Elements
        {
            get
            {
                return Enumerable.Empty<IXmlElement>();
            }
        }

        public IEnumerable<KeyValuePair<string, string>> Attributes
        {
            get
            {
                if (AttributesNode == null)
                {
                    yield break;
                }

                var singleAttribute = AttributesNode as XmlAttributeSyntax;
                if (singleAttribute != null)
                {
                    yield return new KeyValuePair<string, string>(singleAttribute.Name, singleAttribute.Value);
                    yield break;
                }

                foreach (var attribute in AttributesNode.ChildNodes.OfType<XmlAttributeSyntax>())
                {
                    yield return new KeyValuePair<string, string>(attribute.Name, attribute.Value);
                }
            }
        }

        public string this[string attributeName]
        {
            get
            {
                foreach (var attribute in Attributes)
                {
                    if (attribute.Key == attributeName)
                    {
                        return attribute.Value;
                    }
                }

                return null;
            }
        }
    }
}
