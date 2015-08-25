namespace Microsoft.Language.Xml
{
    public enum XmlClassificationTypes : byte
    {
        None,
        XmlAttributeName,
        XmlAttributeQuotes,
        XmlAttributeValue,
        XmlCDataSection,
        XmlComment,
        XmlDelimiter,
        XmlEntityReference,
        XmlName,
        XmlProcessingInstruction,
        XmlText,
        Count,
    }
}
