using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.Language.Xml.Comments;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Language.Xml.Editor
{
    // TODO: Finish implementing and testing logic here
    //[Export(typeof(CommentingService))]
    public class CommentingService
    {
        [Import]
        private ParserService parserService = null;

        public void CommentSelection(ITextView textView)
        {
            var snapshot = textView.TextSnapshot;
            var treeTask = parserService.GetSyntaxTree(snapshot);
            treeTask.Wait(100);

            if (!treeTask.IsCompleted)
            {
                return;
            }

            var root = treeTask.Result;
            var selection = textView.Selection;

            List<TextSpan> commentSpans = new List<TextSpan>();

            foreach (var selectedSpan in selection.SelectedSpans)
            {
                var desiredCommentSpan = GetDesiredCommentSpan(snapshot, selectedSpan);
                commentSpans.AddRange(root.GetValidCommentSpans(desiredCommentSpan));
            }

            var textBuffer = textView.TextBuffer;

            using (var edit = textBuffer.CreateEdit())
            {
                for (int i = commentSpans.Count - 1; i >= 0; i--)
                {
                    var commentSpan = commentSpans[i];
                    edit.Insert(commentSpan.End, "-->");
                    edit.Insert(commentSpan.Start, "<!--");
                }

                edit.Apply();
            }
        }

        private static TextSpan GetDesiredCommentSpan(ITextSnapshot snapshot, SnapshotSpan selectedSpan)
        {
            if (selectedSpan.IsEmpty)
            {
                // Comment line for empty selections (first to last non-whitespace character)
                var line = selectedSpan.Snapshot.GetLineFromPosition(selectedSpan.Start);

                int? start = null;
                for (int i = line.Start; i < line.End.Position; i++)
                {
                    if (!Scanner.IsWhitespace(snapshot[i]))
                    {
                        start = i;
                        break;
                    }
                }

                if (start == null)
                {
                    return new TextSpan(selectedSpan.Start, 0);
                }
                else
                {
                    int end = start.Value;
                    for (int i = line.End.Position - 1; i >= end; i--)
                    {
                        if (!Scanner.IsWhitespace(snapshot[i]))
                        {
                            end = i;
                            break;
                        }
                    }

                    return new TextSpan(start.Value, end);
                }
            }
            else
            {
                return new TextSpan(selectedSpan.Start, selectedSpan.Length);
            }
        }

        public void UncommentSelection()
        {

        }
    }
}
