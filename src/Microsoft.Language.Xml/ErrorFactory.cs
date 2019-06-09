using System;

namespace Microsoft.Language.Xml
{
    public class ErrorFactory
    {
        internal static DiagnosticInfo ErrorInfo(ERRID errID)
        {
            return new DiagnosticInfo(errID);
        }

        internal static DiagnosticInfo ErrorInfo(ERRID errID, object[] arguments)
        {
            return new DiagnosticInfo(errID, arguments);
        }

        internal static DiagnosticInfo ErrorInfo(ERRID errID, char xmlCh, string v)
        {
            return new DiagnosticInfo(errID, new object[] { xmlCh, v });
        }

        internal static DiagnosticInfo ErrorInfo(ERRID errID, string xmlCh, string v)
        {
            return new DiagnosticInfo(errID, new object[] { xmlCh, v });
        }
    }
}
