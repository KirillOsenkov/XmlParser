using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;

namespace Microsoft.Language.Xml.Editor
{
    [Export]
    public class ParserService
    {
        public Task<XmlNodeSyntax> GetSyntaxTree(ITextSnapshot textSnapshot)
        {
            var textBuffer = textSnapshot.TextBuffer;

            lock (textBuffer)
            {
                ConditionalWeakTable<ITextSnapshot, Task<XmlNodeSyntax>> textSnapshotToSyntaxRootMap;
                Task<XmlNodeSyntax> syntaxRootTask;

                if (!textBuffer.Properties.TryGetProperty(typeof(ParserService), out textSnapshotToSyntaxRootMap))
                {
                    textSnapshotToSyntaxRootMap = new ConditionalWeakTable<ITextSnapshot, Task<XmlNodeSyntax>>();
                    textBuffer.Properties.AddProperty(typeof(ParserService), textSnapshotToSyntaxRootMap);
                }
                else if (textSnapshotToSyntaxRootMap.TryGetValue(textSnapshot, out syntaxRootTask))
                {
                    return syntaxRootTask;
                }

                syntaxRootTask = Task.Run(() => Parse(textSnapshot));
                textSnapshotToSyntaxRootMap.Add(textSnapshot, syntaxRootTask);

                return syntaxRootTask;
            }
        }

        private static XmlNodeSyntax Parse(ITextSnapshot snapshot)
        {
            var parser = new Parser(new TextSnapshotBuffer(snapshot));
            return parser.Parse();
        }
    }
}
