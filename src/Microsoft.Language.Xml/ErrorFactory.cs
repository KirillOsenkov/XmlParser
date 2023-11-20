namespace Microsoft.Language.Xml
{
    internal static class ErrorFactory
    {
        public static DiagnosticInfo ErrorInfo(ERRID errID)
        {
            return new DiagnosticInfo(errID);
        }

        public static DiagnosticInfo ErrorInfo(ERRID errID, object[] arguments)
        {
            return new DiagnosticInfo(errID, arguments);
        }

        public static DiagnosticInfo ErrorInfo(ERRID errID, char xmlCh, string v)
        {
            return new DiagnosticInfo(errID, new object[] { xmlCh, v });
        }

        public static DiagnosticInfo ErrorInfo(ERRID errID, string xmlCh, string v)
        {
            return new DiagnosticInfo(errID, new object[] { xmlCh, v });
        }
    }
}
