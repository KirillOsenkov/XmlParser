using Microsoft.VisualStudio.Text;

namespace Microsoft.Language.Xml
{
    public class TextSnapshotBuffer : Buffer
    {
        private ITextSnapshot textSnapshot;

        public TextSnapshotBuffer(ITextSnapshot textSnapshot)
        {
            this.textSnapshot = textSnapshot;
        }

        public override int Length
        {
            get { return textSnapshot.Length; }
        }

        public override char this[int index]
        {
            get
            {
                return textSnapshot[index];
            }
        }

        public override string GetText(int start, int length)
        {
            return textSnapshot.GetText(start, length);
        }

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            textSnapshot.CopyTo(sourceIndex, destination, destinationIndex, count);
        }
    }
}
