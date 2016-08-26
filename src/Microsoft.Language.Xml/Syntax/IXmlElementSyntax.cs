using System.Collections.Generic;

namespace Microsoft.Language.Xml
{
    public interface IXmlElementSyntax
    {
        XmlNameSyntax Name { get; }
        SyntaxNode Content { get; }
        IXmlElementSyntax Parent { get; }
        IEnumerable<IXmlElementSyntax> Elements { get; }
        IEnumerable<XmlAttributeSyntax> Attributes { get; }
        XmlAttributeSyntax this[string attributeName] { get; }
        IXmlElementSyntax AsSyntaxElement { get; }
        IXmlElement AsElement { get; }
    }
}
