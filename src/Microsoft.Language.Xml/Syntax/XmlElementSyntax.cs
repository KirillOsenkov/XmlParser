using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Language.Xml
{
    public class XmlElementSyntax : XmlNodeSyntax, IXmlElement
    {
        public XmlElementStartTagSyntax StartTag { get; set; }
        public XmlElementEndTagSyntax EndTag { get; set; }
        public SyntaxNode Content { get; set; }

        public XmlElementSyntax(XmlElementStartTagSyntax start, SyntaxNode content, XmlElementEndTagSyntax end) : base(SyntaxKind.XmlElement)
        {
            StartTag = start;
            Content = content;
            EndTag = end;
            SlotCount = 3;
        }

        public override SyntaxNode GetSlot(int index)
        {
            switch (index)
            {
                case 0: return StartTag;
                case 1: return Content;
                case 2: return EndTag;
                default:
                    throw new InvalidOperationException();
            }
        }

        internal override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlElement(this);
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

        public string Name
        {
            get
            {
                string name = null;

                if (StartTag != null)
                {
                    name = StartTag.Name;
                }

                if (name == null && EndTag != null)
                {
                    return EndTag.Name;
                }

                return name;
            }
        }

        public IEnumerable<IXmlElement> Elements
        {
            get
            {
                if (Content is SyntaxList)
                {
                    return ((SyntaxList)Content).ChildNodes.OfType<IXmlElement>();
                }

                return Enumerable.Empty<IXmlElement>();
            }
        }

        public IEnumerable<KeyValuePair<string, string>> Attributes
        {
            get
            {
                if (StartTag == null || StartTag.Attributes == null)
                {
                    yield break;
                }

                var singleAttribute = StartTag.Attributes as XmlAttributeSyntax;
                if (singleAttribute != null)
                {
                    yield return new KeyValuePair<string, string>(singleAttribute.Name, singleAttribute.Value);
                    yield break;
                }

                foreach (var attribute in StartTag.Attributes.ChildNodes.OfType<XmlAttributeSyntax>())
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
