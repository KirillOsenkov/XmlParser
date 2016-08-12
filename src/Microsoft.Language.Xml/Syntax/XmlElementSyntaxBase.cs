using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Language.Xml
{
    public abstract class XmlElementSyntaxBase : XmlNodeSyntax, IXmlElement, IXmlElementSyntax
    {
        public XmlNameSyntax NameNode;
        public SyntaxNode AttributesNode;

        public XmlElementSyntaxBase(
            SyntaxKind syntaxKind,
            XmlNameSyntax name,
            SyntaxNode attributes) : base(syntaxKind)
        {
            this.NameNode = name;
            this.AttributesNode = attributes;
        }

        protected abstract IEnumerable<IXmlElementSyntax> SyntaxElements { get; }

        public abstract SyntaxNode Content { get; }

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
                return SyntaxElements.Select(el => el.AsElement);
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

        public string Value
        {
            get
            {
                return Content?.ToFullString() ?? "";
            }
        }

        XmlNameSyntax IXmlElementSyntax.Name
        {
            get
            {
                return NameNode;
            }
        }


        public IXmlElementSyntax AsSyntaxElement
        {
            get
            {
                return this;
            }
        }

        IXmlElementSyntax IXmlElementSyntax.Parent
        {
            get
            {
                var current = this.Parent;
                while (current != null)
                {
                    if (current is IXmlElementSyntax)
                    {
                        return current as IXmlElementSyntax;
                    }

                    current = current.Parent;
                }

                return null;
            }
        }

        IEnumerable<IXmlElementSyntax> IXmlElementSyntax.Elements
        {
            get
            {
                return SyntaxElements;
            }
        }

        IEnumerable<XmlAttributeSyntax> IXmlElementSyntax.Attributes
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
                    yield return singleAttribute;
                    yield break;
                }

                foreach (var attribute in AttributesNode.ChildNodes.OfType<XmlAttributeSyntax>())
                {
                    yield return attribute;
                }
            }
        }

        public IXmlElement AsElement
        {
            get
            {
                return this;
            }
        }

        XmlAttributeSyntax IXmlElementSyntax.this[string attributeName]
        {
            get
            {
                foreach (var attribute in AsSyntaxElement.Attributes)
                {
                    if (attribute.Name == attributeName)
                    {
                        return attribute;
                    }
                }

                return null;
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
