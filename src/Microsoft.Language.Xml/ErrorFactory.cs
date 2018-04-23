using System;

namespace Microsoft.Language.Xml
{
    public class DiagnosticInfo
    {
    }

    public class ErrorFactory
    {
        internal static DiagnosticInfo ErrorInfo(ERRID errID)
        {
            return new DiagnosticInfo();
        }

        internal static DiagnosticInfo ErrorInfo(ERRID id, char xmlCh, string v)
        {
            return new DiagnosticInfo();
        }

        internal static DiagnosticInfo ErrorInfo(ERRID id, string xmlCh, string v)
        {
            return new DiagnosticInfo();
        }
    }
}
