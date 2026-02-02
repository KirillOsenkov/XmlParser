using System.Collections.Generic;

namespace Microsoft.Language.Xml
{
    public interface IXmlElementSyntax
    {
        string Name { get; }
        XmlNameSyntax NameNode { get; }
        SyntaxList<SyntaxNode> Content { get; }
        IXmlElementSyntax? Parent { get; }
        IEnumerable<IXmlElementSyntax> Elements { get; }
        IEnumerable<XmlAttributeSyntax>? Attributes { get; }
        SyntaxList<XmlAttributeSyntax> AttributesNode { get; }
        XmlAttributeSyntax? GetAttribute(string localName, string? prefix = null);
        string? GetAttributeValue(string localName, string? prefix = null);
        IXmlElement AsElement { get; }
        XmlNodeSyntax AsNode { get; }
        string ToFullString();

        IXmlElementSyntax WithName(XmlNameSyntax newName);
        IXmlElementSyntax WithContent(SyntaxList<SyntaxNode> newContent);
        IXmlElementSyntax WithAttributes(IEnumerable<XmlAttributeSyntax> newAttributes);
        IXmlElementSyntax WithAttributes(SyntaxList<XmlAttributeSyntax> newAttributes);
    }
}
