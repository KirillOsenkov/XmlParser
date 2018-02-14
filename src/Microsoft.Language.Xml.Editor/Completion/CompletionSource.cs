using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.Language.Xml.Editor
{
	public class CompletionSource : ICompletionSource
	{
		ITextBuffer textBuffer;
		ParserService parserService;
		IEnumerable<IXmlCompletionProvider> providers;

		public CompletionSource(ITextBuffer textBuffer,
								ParserService parserService,
								IEnumerable<IXmlCompletionProvider> providers)
		{
			this.textBuffer = textBuffer;
			this.parserService = parserService;
			this.providers = providers;
		}

		public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
		{
			var snapshot = textBuffer.CurrentSnapshot;
			SnapshotPoint? triggerPoint = session.GetTriggerPoint(snapshot);
			int position = triggerPoint.Value.Position;
			var span = new Span(position, 0);
			ITrackingSpan typingSpan = snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);

			var tree = parserService.TryGetSyntaxTree(snapshot);
			if (tree == null)
				return;
			var node = tree.FindNode(position);
			IEnumerable<CompletionSet> resultSets = null;
			if (node == null)
			{
				resultSets = GetCompletionSet(
					typingSpan,
					p => p.GetElementCompletions()
				);
			}
			else if (node.Kind == SyntaxKind.XmlElement || node.Kind == SyntaxKind.XmlEmptyElement)
			{
				resultSets = GetCompletionSet(
					typingSpan,
					p => p.GetAttributeCompletions((IXmlElement)node)
				);
			}
			else if (node.Kind == SyntaxKind.XmlAttribute)
			{

			}
				

			if (resultSets != null)
				foreach (var set in resultSets)
					completionSets.Add(set);

			/*var completions = new List<Completion>();
			var completion = new Completion("foo", "foo", "this is foo", null, "");
			completions.Add(completion);
			var completionSet = new CompletionSet("XML", "XML", typingSpan, completions, null);
			completionSets.Add(completionSet);*/
		}

		IEnumerable<CompletionSet> GetCompletionSet(ITrackingSpan trackingSpan,
		                                            Func<IXmlCompletionProvider, IEnumerable<string>> getter)
		{
			foreach (var provider in providers)
			{
				var textCompletions = getter(provider);
				if (textCompletions == null)
					continue;
				var completions = textCompletions.Select(tc => new Completion3(tc, tc, null, null, tc));
				var completionSet = new CompletionSet(provider.Name,
				                                      provider.DisplayName,
				                                      trackingSpan,
				                                      completions,
				                                      null);
				yield return completionSet;
			}
		}

		CompletionType GetCompletionType(XmlNodeSyntax node, int position)
		{
			// If nothing can be found, complete element
			if (node == null)
				return CompletionType.Element;
			// If the cursor is in an empty element, simply complete attributes
			if (node.Kind == SyntaxKind.XmlEmptyElement)
				return CompletionType.Attribute;

			IXmlElement element = null;
			XmlAttributeSyntax attr = null;

			if (node.Kind == SyntaxKind.XmlElement)
				element = (IXmlElement)node;
			if (node.Kind == SyntaxKind.XmlAttribute)
				attr = (XmlAttributeSyntax)node;

			if (node == null)
			{
				return CompletionType.Element;
			}
			else if (element != null)
			{
				return CompletionType.Attribute;
			}
			else if (node.Kind == SyntaxKind.XmlAttribute)
			{
				return CompletionType.AttributeValue;
			}

			return CompletionType.Element;
		}

		enum CompletionType
		{
			Element,
			Attribute,
			AttributeValue
		}

		public void Dispose()
		{

		}
	}
}
