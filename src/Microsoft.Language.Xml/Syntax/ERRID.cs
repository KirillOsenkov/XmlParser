namespace Microsoft.Language.Xml
{
    public enum ERRID
    {
        //Void = InternalErrorCode.Void,
        //Unknown = InternalErrorCode.Unknown,
        ERR_None = 0,

        ERR_Syntax = 30035,
        ERR_IllegalChar = 30037,
        ERR_ExpectedGreater = 30636,
        ERR_ExpectedXmlName = 31146,
        ERR_DuplicateXmlAttribute = 31149,
        ERR_MismatchedXmlEndTag = 31150,
        ERR_MissingXmlEndTag = 31151,
        ERR_MissingVersionInXmlDecl = 31153,
        ERR_IllegalAttributeInXmlDecl = 31154,
        ERR_VersionMustBeFirstInXmlDecl = 31156,
        ERR_AttributeOrder = 31157,
        ERR_ExpectedSQuote = 31163,
        ERR_ExpectedQuote = 31164,
        ERR_ExpectedLT = 31165,
        ERR_StartAttributeValue = 31166,
        ERR_IllegalXmlStartNameChar = 31169,
        ERR_IllegalXmlNameChar = 31170,
        ERR_IllegalXmlCommentChar = 31171,
        ERR_ExpectedXmlWhiteSpace = 31173,
        ERR_IllegalProcessingInstructionName = 31174,
        ERR_DTDNotSupported = 31175,
        ERR_IllegalXmlWhiteSpace = 31177,
        ERR_ExpectedSColon = 31178,
        ERR_XmlEntityReference = 31180,
        ERR_InvalidAttributeValue1 = 31181,
        ERR_InvalidAttributeValue2 = 31182,
        ERR_XmlEndCDataNotAllowedInContent = 31198,
        ERR_XmlEndElementNoMatchingStart = 31207,

        ERR_LastPlusOne
    }
}
