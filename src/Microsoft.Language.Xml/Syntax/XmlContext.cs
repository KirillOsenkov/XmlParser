using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Language.Xml
{
    public struct XmlContext
    {
        private XmlElementStartTagSyntax _start;
        private SyntaxListBuilder<XmlNodeSyntax> _content;
        private SyntaxListPool _pool;
        public XmlContext(SyntaxListPool pool, XmlElementStartTagSyntax start)
        {
            _pool = pool;
            _start = start;
            _content = _pool.Allocate<XmlNodeSyntax>();
        }

        public void Add(XmlNodeSyntax xml)
        {
            _content.Add(xml);
        }

        public XmlElementStartTagSyntax StartElement
        {
            get
            {
                return _start;
            }
        }

        public XmlNodeSyntax CreateElement(XmlElementEndTagSyntax endElement)
        {
            Debug.Assert(endElement != null);
            var contentList = _content.ToList();
            _pool.Free(_content);
            return SyntaxFactory.XmlElement(_start, contentList, endElement);
        }

        internal XmlNodeSyntax CreateElement(XmlElementEndTagSyntax missingEndElement, object v)
        {
            return CreateElement(missingEndElement);
        }
    }
}
