using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.Language.Xml.Editor
{
    public class Classifier : AbstractSyntaxTreeTagger, IClassifier
    {
        private static readonly IList<ClassificationSpan> emptySpanList = new ClassificationSpan[0];

        private readonly IClassificationType[] types;

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        public Classifier(IClassificationType[] types, ParserService parserService)
            : base(parserService)
        {
            this.types = types;
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            // don't classify documents larger than 5 MB, arbitrary large number
            if (span.Snapshot.Length > 5000000)
            {
                return emptySpanList;
            }

            var task = parserService.GetSyntaxTree(span.Snapshot);

            // wait for 100 milliseconds to see if we're lucky and it finishes before that
            // this helps significantly reduce flicker since we're not going to clear and re-add all tags on every keystroke
            task.Wait(100);

            if (task.Status == TaskStatus.RanToCompletion)
            {
                var root = task.Result;
                var spans = new List<ClassificationSpan>();
                ClassifierVisitor.Visit(
                    root,
                    span.Start,
                    span.Length,
                    (start, length, node, xmlClassification) => spans.Add(
                        new ClassificationSpan(
                            new SnapshotSpan(
                                span.Snapshot,
                                start,
                                length),
                            types[(int)xmlClassification])));
                return spans;
            }

            task.ContinueWith(t =>
            {
                RaiseClassificationChanged(span.Snapshot);
            }, TaskContinuationOptions.OnlyOnRanToCompletion);

            return emptySpanList;
        }

        private void RaiseClassificationChanged(ITextSnapshot snapshot)
        {
            var handler = ClassificationChanged;
            if (handler != null)
            {
                handler(this, new ClassificationChangedEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
            }
        }
    }
}
