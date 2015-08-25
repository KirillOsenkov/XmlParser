namespace Microsoft.Language.Xml
{
    /// <summary>
    /// Abstraction around text snapshot to decouple the parser from the editor
    /// </summary>
    public class StringBuffer : Buffer
    {
        private string text;

        public StringBuffer(string text)
        {
            this.text = text;
        }

        public override int Length
        {
            get { return text.Length; }
        }

        public override char this[int index]
        {
            get
            {
                return text[index];
            }
        }

        public override string GetText(int start, int length)
        {
            return text.Substring(start, length);
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            text.CopyTo(sourceIndex, destination, destinationIndex, count);
        }
    }
}
