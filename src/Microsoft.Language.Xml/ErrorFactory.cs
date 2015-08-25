using System;

namespace Microsoft.Language.Xml
{
    public class ErrorFactory
    {
        internal static DiagnosticInfo ErrorInfo(ERRID eRR_ExpectedSColon)
        {
            return new DiagnosticInfo();
        }

        internal static DiagnosticInfo ErrorInfo(ERRID id, char xmlCh, string v)
        {
            return new DiagnosticInfo();
        }
    }
}
