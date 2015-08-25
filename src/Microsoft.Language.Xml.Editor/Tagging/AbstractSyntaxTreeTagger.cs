namespace Microsoft.Language.Xml.Editor
{
    public abstract class AbstractSyntaxTreeTagger
    {
        protected readonly ParserService parserService;

        public AbstractSyntaxTreeTagger(ParserService parserService)
        {
            this.parserService = parserService;
        }
    }
}
