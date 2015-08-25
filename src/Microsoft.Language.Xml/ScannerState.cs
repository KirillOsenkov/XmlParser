namespace Microsoft.Language.Xml
{
    public enum ScannerState
    {
        Content, // Scan text between markup
        Misc, // Scan tokens in Xml misc state, these are tokens between document declaration and the root element
        DocType, // Scan tokens inside of <!DOCTYPE ... >
        Element, // Scan tokens inside of < ... >
        EndElement, // Scan tokens inside of </ ...>
        SingleQuotedString, // Scan a single quoted string
        SmartSingleQuotedString, // Scan a single quoted string DWCH_RSMART_Q
        QuotedString, // Scan a quoted string
        SmartQuotedString, // Scan a quoted string DWCH_RSMART_DQ
        UnQuotedString, // Scan a string that is missing quotes (error recovery)
        CData, // Scan text inside of <![CDATA[ ... ]]>
        StartProcessingInstruction, // Scan first text inside f <? ... ?>, the first text can have leading trivia
        ProcessingInstruction, // Scan remaining text inside of <? ... ?>
        Comment, // Scan text inside of <!-- ... -->
    }
}
