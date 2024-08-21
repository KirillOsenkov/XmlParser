using System.Diagnostics;

namespace Microsoft.Language.Xml
{
    using static SyntaxFactory;

    public class SyntaxRewriter : SyntaxVisitor
    {
        public override SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> list)
        {
            SyntaxListBuilder<TNode> alternate = default(SyntaxListBuilder<TNode>);
            int i = 0;
            int n = list.Count;
            while (i < n)
            {
                TNode item = list[i];
                TNode visited = ((TNode)this.Visit(item));
                if (item != visited && alternate.IsNull)
                {
                    alternate = new SyntaxListBuilder<TNode>(n);
                    alternate.AddRange(list, 0, i);
                }

                if (!alternate.IsNull)
                {
                    if (visited != null && visited.Kind != SyntaxKind.None)
                    {
                        alternate.Add(visited);
                    }
                }

                i += 1;
            }

            if (!alternate.IsNull)
            {
                return alternate.ToList();
            }

            return list;
        }

        public virtual SeparatedSyntaxList<TNode> VisitList<TNode>(SeparatedSyntaxList<TNode> list) where TNode : SyntaxNode
        {
            SeparatedSyntaxListBuilder<TNode> alternate = default(SeparatedSyntaxListBuilder<TNode>);
            int i = 0;
            int itemCount = list.Count;
            int separatorCount = list.SeparatorCount;
            while (i < itemCount)
            {
                var item = list[i];
                var visitedItem = this.Visit(item);

                SyntaxToken? separator = null;
                SyntaxToken? visitedSeparator = null;

                if (i < separatorCount)
                {
                    separator = list.GetSeparator(i);
                    var visitedSeparatorNode = this.Visit(separator);

                    Debug.Assert(visitedSeparatorNode is SyntaxToken, "Cannot replace a separator with a non-separator");

                    visitedSeparator = (SyntaxToken)visitedSeparatorNode;

                    Debug.Assert((separator == null &&
                        separator.Kind == SyntaxKind.None) ||
                        (visitedSeparator != null &&
                        visitedSeparator.Kind != SyntaxKind.None),
                        "Cannot delete a separator from a separated list. Removing an element will remove the corresponding separator.");
                }

                if (item != visitedItem && alternate.IsNull)
                {
                    alternate = new SeparatedSyntaxListBuilder<TNode>(itemCount);
                    alternate.AddRange(list, i);
                }

                if (!alternate.IsNull)
                {
                    if (visitedItem != null && visitedItem.Kind != SyntaxKind.None)
                    {
                        alternate.Add(((TNode)visitedItem));
                        if (visitedSeparator != null)
                        {
                            alternate.AddSeparator(visitedSeparator);
                        }
                    }
                    else if (i >= separatorCount && alternate.Count > 0)
                    {
                        alternate.RemoveLast(); // delete *preceding* separator
                    }
                }

                i += 1;
            }

            if (!alternate.IsNull)
            {
                return alternate.ToList();
            }

            return list;
        }

        public override SyntaxNode VisitXmlDocument(XmlDocumentSyntax node)
        {
            bool anyChanges = false;
            var newDeclaration = ((XmlDeclarationSyntax?)Visit(node.Prologue));
            if (node.Prologue != newDeclaration)
            {
                anyChanges = true;
            }

            var newPrecedingMisc = VisitList<SyntaxNode>(node.PrecedingMisc);
            if (node.PrecedingMisc.Node != newPrecedingMisc.Node)
            {
                anyChanges = true;
            }

            var newRoot = ((XmlNodeSyntax?)Visit(node.Body));
            if (node.Body != newRoot)
            {
                anyChanges = true;
            }

            var newFollowingMisc = VisitList<SyntaxNode>(node.FollowingMisc);
            if (node.FollowingMisc.Node != newFollowingMisc.Node)
            {
                anyChanges = true;
            }

            var newEof = VisitSyntaxToken(node.Eof);
            if (node.Eof != newEof)
            {
                anyChanges = true;
            }

            var newSkippedTokens = ((SkippedTokensTriviaSyntax)VisitSkippedTokensTrivia(node.SkippedTokens));
            if (node.SkippedTokens != newSkippedTokens)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return node.CopyAnnotationsTo(XmlDocument(
                    newDeclaration,
                    newPrecedingMisc.Node,
                    newRoot,
                    newFollowingMisc.Node,
                    newSkippedTokens,
                    newEof));
            }
            else
            {
                return node;
            }
        }

        public override SyntaxNode VisitXmlDeclaration(XmlDeclarationSyntax node)
        {
            bool anyChanges = false;
            var newLessThanQuestionToken = ((PunctuationSyntax?)Visit(node.LessThanQuestionToken));
            if (node.LessThanQuestionToken != newLessThanQuestionToken)
            {
                anyChanges = true;
            }

            var newXmlKeyword = ((KeywordSyntax?)Visit(node.XmlKeyword));
            if (node.XmlKeyword != newXmlKeyword)
            {
                anyChanges = true;
            }

            var newVersion = ((XmlDeclarationOptionSyntax?)Visit(node.Version));
            if (node.Version != newVersion)
            {
                anyChanges = true;
            }

            var newEncoding = ((XmlDeclarationOptionSyntax?)Visit(node.Encoding));
            if (node.Encoding != newEncoding)
            {
                anyChanges = true;
            }

            var newStandalone = ((XmlDeclarationOptionSyntax?)Visit(node.Standalone));
            if (node.Standalone != newStandalone)
            {
                anyChanges = true;
            }

            var newQuestionGreaterThanToken = ((PunctuationSyntax?)Visit(node.QuestionGreaterThanToken));
            if (node.QuestionGreaterThanToken != newQuestionGreaterThanToken)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return node.CopyAnnotationsTo(XmlDeclaration(
                    newLessThanQuestionToken,
                    newXmlKeyword,
                    newVersion,
                    newEncoding,
                    newStandalone,
                    newQuestionGreaterThanToken));
            }
            else
            {
                return node;
            }
        }

        public override SyntaxNode VisitXmlDeclarationOption(XmlDeclarationOptionSyntax node)
        {
            bool anyChanges = false;
            var newName = ((XmlNameTokenSyntax?)Visit(node.NameNode));
            if (node.NameNode != newName)
            {
                anyChanges = true;
            }

            var newEquals = ((PunctuationSyntax?)Visit(node.Equals));
            if (node.Equals != newEquals)
            {
                anyChanges = true;
            }

            var newValue = ((XmlStringSyntax?)Visit(node.Value));
            if (node.Value != newValue)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return node.CopyAnnotationsTo(XmlDeclarationOption(newName, newEquals, newValue));
            }
            else
            {
                return node;
            }
        }

        public override SyntaxNode VisitXmlElement(XmlElementSyntax node)
        {
            bool anyChanges = false;
            var newStartTag = ((XmlElementStartTagSyntax?)Visit(node.StartTag));
            if (node.StartTag != newStartTag)
            {
                anyChanges = true;
            }

            var newContent = VisitList<SyntaxNode>(node.Content);
            if (node.Content.Node != newContent.Node)
            {
                anyChanges = true;
            }

            var newEndTag = ((XmlElementEndTagSyntax?)Visit(node.EndTag));
            if (node.EndTag != newEndTag)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return node.CopyAnnotationsTo(XmlElement(newStartTag, newContent.Node, newEndTag));
            }
            else
            {
                return node;
            }
        }

        public override SyntaxNode VisitXmlText(XmlTextSyntax node)
        {
            bool anyChanges = false;
            var newTextTokens = VisitList<SyntaxNode>(node.TextTokens);
            if (node.TextTokens.Node != newTextTokens.Node)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return node.CopyAnnotationsTo(XmlText(newTextTokens.Node));
            }
            else
            {
                return node;
            }
        }

        public override SyntaxNode VisitXmlElementStartTag(XmlElementStartTagSyntax node)
        {
            bool anyChanges = false;
            var newLessThanToken = ((PunctuationSyntax?)Visit(node.LessThanToken));
            if (node.LessThanToken != newLessThanToken)
            {
                anyChanges = true;
            }

            var newName = ((XmlNameSyntax?)Visit(node.NameNode));
            if (node.NameNode != newName)
            {
                anyChanges = true;
            }

            var newAttributes = VisitList<SyntaxNode>(node.AttributesNode);
            if (node.AttributesNode.Node != newAttributes.Node)
            {
                anyChanges = true;
            }

            var newGreaterThanToken = ((PunctuationSyntax?)Visit(node.GreaterThanToken));
            if (node.GreaterThanToken != newGreaterThanToken)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return node.CopyAnnotationsTo(XmlElementStartTag(newLessThanToken, newName, newAttributes.Node, newGreaterThanToken));
            }
            else
            {
                return node;
            }
        }

        public override SyntaxNode VisitXmlElementEndTag(XmlElementEndTagSyntax node)
        {
            bool anyChanges = false;
            var newLessThanSlashToken = ((PunctuationSyntax?)Visit(node.LessThanSlashToken));
            if (node.LessThanSlashToken != newLessThanSlashToken)
            {
                anyChanges = true;
            }

            var newName = ((XmlNameSyntax?)Visit(node.NameNode));
            if (node.NameNode != newName)
            {
                anyChanges = true;
            }

            var newGreaterThanToken = ((PunctuationSyntax?)Visit(node.GreaterThanToken));
            if (node.GreaterThanToken != newGreaterThanToken)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return node.CopyAnnotationsTo(XmlElementEndTag(newLessThanSlashToken, newName, newGreaterThanToken));
            }
            else
            {
                return node;
            }
        }

        public override SyntaxNode VisitXmlEmptyElement(XmlEmptyElementSyntax node)
        {
            bool anyChanges = false;
            var newLessThanToken = ((PunctuationSyntax?)Visit(node.LessThanToken));
            if (node.LessThanToken != newLessThanToken)
            {
                anyChanges = true;
            }

            var newName = ((XmlNameSyntax?)Visit(node.NameNode));
            if (node.NameNode != newName)
            {
                anyChanges = true;
            }

            var newAttributes = VisitList<SyntaxNode>(node.AttributesNode);
            if (node.AttributesNode.Node != newAttributes.Node)
            {
                anyChanges = true;
            }

            var newSlashGreaterThanToken = ((PunctuationSyntax?)Visit(node.SlashGreaterThanToken));
            if (node.SlashGreaterThanToken != newSlashGreaterThanToken)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return node.CopyAnnotationsTo(XmlEmptyElement(newLessThanToken, newName, newAttributes.Node, newSlashGreaterThanToken));
            }
            else
            {
                return node;
            }
        }

        public override SyntaxNode VisitXmlAttribute(XmlAttributeSyntax node)
        {
            bool anyChanges = false;
            var newName = ((XmlNameSyntax?)Visit(node.NameNode));
            if (node.NameNode != newName)
            {
                anyChanges = true;
            }

            var newEqualsToken = ((PunctuationSyntax?)Visit(node.Equals));
            if (node.Equals != newEqualsToken)
            {
                anyChanges = true;
            }

            var newValue = ((XmlNodeSyntax?)Visit(node.ValueNode));
            if (node.ValueNode != newValue)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return node.CopyAnnotationsTo(XmlAttribute(newName, newEqualsToken, newValue));
            }
            else
            {
                return node;
            }
        }

        public override SyntaxNode VisitXmlString(XmlStringSyntax node)
        {
            bool anyChanges = false;
            var newStartQuoteToken = ((PunctuationSyntax?)Visit(node.StartQuoteToken));
            if (node.StartQuoteToken != newStartQuoteToken)
            {
                anyChanges = true;
            }

            var newTextTokens = VisitList(node.TextTokens);
            if (node.TextTokens.Node != newTextTokens.Node)
            {
                anyChanges = true;
            }

            var newEndQuoteToken = ((PunctuationSyntax?)Visit(node.EndQuoteToken));
            if (node.EndQuoteToken != newEndQuoteToken)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return node.CopyAnnotationsTo(XmlString(newStartQuoteToken, newTextTokens.Node, newEndQuoteToken));
            }
            else
            {
                return node;
            }
        }

        public override SyntaxNode VisitXmlName(XmlNameSyntax node)
        {
            bool anyChanges = false;
            var newPrefix = ((XmlPrefixSyntax?)Visit(node.PrefixNode));
            if (node.PrefixNode != newPrefix)
            {
                anyChanges = true;
            }

            var newLocalName = ((XmlNameTokenSyntax?)Visit(node.LocalNameNode));
            if (node.LocalNameNode != newLocalName)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return node.CopyAnnotationsTo(XmlName(newPrefix, newLocalName));
            }
            else
            {
                return node;
            }
        }

        public override SyntaxNode VisitXmlPrefix(XmlPrefixSyntax node)
        {
            bool anyChanges = false;
            var newName = ((XmlNameTokenSyntax?)Visit(node.Name));
            if (node.Name != newName)
            {
                anyChanges = true;
            }

            var newColonToken = ((PunctuationSyntax?)Visit(node.ColonToken));
            if (node.ColonToken != newColonToken)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return node.CopyAnnotationsTo(XmlPrefix(newName, newColonToken));
            }
            else
            {
                return node;
            }
        }

        public override SyntaxNode VisitXmlComment(XmlCommentSyntax node)
        {
            bool anyChanges = false;
            var newLessThanExclamationMinusMinusToken = ((PunctuationSyntax?)Visit(node.BeginComment));
            if (node.BeginComment != newLessThanExclamationMinusMinusToken)
            {
                anyChanges = true;
            }

            var newTextTokens = VisitList<SyntaxNode>(node.Content);
            if (node.Content.Node != newTextTokens.Node)
            {
                anyChanges = true;
            }

            var newMinusMinusGreaterThanToken = ((PunctuationSyntax?)Visit(node.EndComment));
            if (node.EndComment != newMinusMinusGreaterThanToken)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return node.CopyAnnotationsTo(XmlComment(newLessThanExclamationMinusMinusToken, newTextTokens.Node, newMinusMinusGreaterThanToken));
            }
            else
            {
                return node;
            }
        }

        public override SyntaxNode VisitXmlProcessingInstruction(XmlProcessingInstructionSyntax node)
        {
            bool anyChanges = false;
            var newLessThanQuestionToken = ((PunctuationSyntax?)Visit(node.LessThanQuestionToken));
            if (node.LessThanQuestionToken != newLessThanQuestionToken)
            {
                anyChanges = true;
            }

            var newName = ((XmlNameTokenSyntax?)Visit(node.Name));
            if (node.Name != newName)
            {
                anyChanges = true;
            }

            var newTextTokens = VisitList<SyntaxNode>(node.TextTokens);
            if (node.TextTokens.Node != newTextTokens.Node)
            {
                anyChanges = true;
            }

            var newQuestionGreaterThanToken = ((PunctuationSyntax?)Visit(node.QuestionGreaterThanToken));
            if (node.QuestionGreaterThanToken != newQuestionGreaterThanToken)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return node.CopyAnnotationsTo(XmlProcessingInstruction(newLessThanQuestionToken, newName, newTextTokens.Node, newQuestionGreaterThanToken));
            }
            else
            {
                return node;
            }
        }

        public override SyntaxNode VisitXmlCDataSection(XmlCDataSectionSyntax node)
        {
            bool anyChanges = false;
            var newBeginCDataToken = ((PunctuationSyntax?)Visit(node.BeginCData));
            if (node.BeginCData != newBeginCDataToken)
            {
                anyChanges = true;
            }

            var newTextTokens = VisitList<SyntaxNode>(node.TextTokens);
            if (node.TextTokens != newTextTokens)
            {
                anyChanges = true;
            }

            var newEndCDataToken = ((PunctuationSyntax?)Visit(node.EndCData));
            if (node.EndCData != newEndCDataToken)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return node.CopyAnnotationsTo(XmlCDataSection(newBeginCDataToken, newTextTokens.Node, newEndCDataToken));
            }
            else
            {
                return node;
            }
        }
    }
}
