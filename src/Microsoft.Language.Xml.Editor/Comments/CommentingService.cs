using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Language.Xml.Comments;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Language.Xml.Editor
{
    // TODO: Finish implementing and testing logic here
    [Export(typeof(CommentingService))]
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
                foreach (var commentSpan in commentSpans)
                {
                    edit.Insert(commentSpan.Start, "<!--");
                    edit.Insert(commentSpan.End, "-->");
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
                            // need to add 1 since end is exclusive
                            end = i + 1;
                            break;
                        }
                    }

                    return TextSpan.FromBounds(start.Value, end);
                }
            }
            else
            {
                return new TextSpan(selectedSpan.Start, selectedSpan.Length);
            }
        }

        public void UncommentSelection(ITextView textView)
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

            List<TextSpan> commentedSpans = new List<TextSpan>();

            int priorCount = 0;
            foreach (var selectedSpan in selection.SelectedSpans)
            {
                bool allowLineUncomment = true;
                if (selectedSpan.IsEmpty)
                {
                    // For point selection, first see which comments are returned for the point span
                    // If the strictly inside a commented node, just uncommented that node
                    // otherwise, allow line uncomment
                    var selectionCommentedSpans = root.GetCommentedSpans(new TextSpan(selectedSpan.Start, 0)).ToList();
                    foreach (var selectionCommentedSpan in selectionCommentedSpans)
                    {
                        if (selectionCommentedSpan.Contains(selectedSpan.Start) &&
                            selectionCommentedSpan.Start != selectedSpan.Start &&
                            selectionCommentedSpan.End != selectedSpan.Start)
                        {
                            commentedSpans.Add(selectionCommentedSpan);
                            allowLineUncomment = false;
                            break;
                        }
                    }
                }

                if (allowLineUncomment)
                {
                    var desiredCommentSpan = GetDesiredCommentSpan(snapshot, selectedSpan);

                    priorCount = commentedSpans.Count;
                    commentedSpans.AddRange(root.GetCommentedSpans(desiredCommentSpan));
                }
            }

            var textBuffer = textView.TextBuffer;

            int beginCommentLength = "<!--".Length;
            int endCommentLength = "-->".Length;

            using (var edit = textBuffer.CreateEdit())
            {
                foreach (var commentSpan in commentedSpans)
                {
                    edit.Delete(commentSpan.Start, beginCommentLength);
                    edit.Delete(commentSpan.End - endCommentLength, endCommentLength);
                }

                edit.Apply();
            }
        }
    }
}
