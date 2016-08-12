using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Language.Xml
{
    public class XmlElementSyntax : XmlElementSyntaxBase
    {
        public XmlElementStartTagSyntax StartTag { get; set; }
        public XmlElementEndTagSyntax EndTag { get; set; }
        public override SyntaxNode Content { get; }

        public XmlElementSyntax(XmlElementStartTagSyntax start, SyntaxNode content, XmlElementEndTagSyntax end) : 
            base(SyntaxKind.XmlElement, start?.NameNode, content)
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

        protected override IEnumerable<IXmlElementSyntax> SyntaxElements
        {
            get
            {
                if (Content is SyntaxList)
                {
                    return ((SyntaxList)Content).ChildNodes.OfType<IXmlElementSyntax>();
                }
                else if (Content is IXmlElementSyntax)
                {
                    return new IXmlElementSyntax[] { (IXmlElementSyntax)Content };
                }

                return Enumerable.Empty<IXmlElementSyntax>();
            }
        }
    }
}
