using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.Language.Xml
{
    using InternalSyntax;
    using static InternalSyntax.SyntaxFactory;

    internal readonly struct XmlContext
    {
        private readonly XmlElementStartTagSyntax.Green _start;
        private readonly InternalSyntax.SyntaxListBuilder<XmlNodeSyntax.Green> _content;
        private readonly SyntaxListPool _pool;

        public XmlContext(SyntaxListPool pool, XmlElementStartTagSyntax.Green start)
        {
            _pool = pool;
            _start = start;
            _content = _pool.Allocate<XmlNodeSyntax.Green>();
        }

        public void Add(XmlNodeSyntax.Green xml)
        {
            _content.Add(xml);
        }

        public XmlElementStartTagSyntax.Green StartElement
        {
            get
            {
                return _start;
            }
        }

        public XmlNodeSyntax.Green CreateElement(XmlElementEndTagSyntax.Green endElement)
        {
            Debug.Assert(endElement != null);
            var contentList = _content.ToList();
            _pool.Free(_content);
            return XmlElement(_start, contentList.Node, endElement);
        }

        internal XmlNodeSyntax.Green CreateElement(XmlElementEndTagSyntax.Green missingEndElement, object v)
        {
            return CreateElement(missingEndElement);
        }
    }
}
