using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.Language.Xml.Editor
{
    public class OutliningTagger : AbstractSyntaxTreeTagger, ITagger<IOutliningRegionTag>
    {
        private static readonly IEnumerable<ITagSpan<IOutliningRegionTag>> emptyTagList = Enumerable.Empty<ITagSpan<IOutliningRegionTag>>();

        private ITextBuffer buffer;
        private OutliningTaggerProvider outliningTaggerProvider;

        public OutliningTagger(OutliningTaggerProvider outliningTaggerProvider, ITextBuffer buffer)
            : base(outliningTaggerProvider.ParserService)
        {
            this.outliningTaggerProvider = outliningTaggerProvider;
            this.buffer = buffer;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
            {
                return emptyTagList;
            }

            var snapshot = spans[0].Snapshot;

            var task = parserService.GetSyntaxTree(snapshot);

            // wait for 100 milliseconds to see if we're lucky and it finishes before that
            // this helps significantly reduce flicker since we're not going to clear and re-add all tags on every keystroke
            task.Wait(100);

            if (task.Status == TaskStatus.RanToCompletion)
            {
                var root = task.Result;
                var elementSpans = new List<Tuple<Span, string>>();
                CollectElementSpans(root, elementSpans, 0);
                var tagSpans = new List<TagSpan<IOutliningRegionTag>>();
                foreach (var span in elementSpans)
                {
                    if (snapshot.GetLineNumberFromPosition(span.Item1.Start) < snapshot.GetLineNumberFromPosition(span.Item1.End))
                    {
                        tagSpans.Add(new TagSpan<IOutliningRegionTag>(
                            new SnapshotSpan(snapshot, span.Item1),
                            new OutliningRegionTag(span.Item2, span.Item2)));
                    }
                }

                return tagSpans;
            }

            task.ContinueWith(t =>
            {
                RaiseTagsChanged(snapshot);
            }, TaskContinuationOptions.OnlyOnRanToCompletion);

            return emptyTagList;
        }

        private void CollectElementSpans(SyntaxNode node, List<Tuple<Span, string>> spans, int start)
        {
            if (node is IXmlElement)
            {
                var leading = node.GetLeadingTriviaWidth();
                var trailing = node.GetTrailingTriviaWidth();
                spans.Add(Tuple.Create(
                    new Span(start + leading, node.FullWidth - leading - trailing),
                    "<" + (node as IXmlElement).Name + ">"));
            }

            foreach (var child in node.ChildNodes)
            {
                CollectElementSpans(child, spans, start);
                start += child.FullWidth;
            }
        }

        private void RaiseTagsChanged(ITextSnapshot snapshot)
        {
            TagsChanged?.Invoke(
                this, 
                new SnapshotSpanEventArgs(
                    new SnapshotSpan(snapshot, 0, snapshot.Length)));
        }
    }
}
