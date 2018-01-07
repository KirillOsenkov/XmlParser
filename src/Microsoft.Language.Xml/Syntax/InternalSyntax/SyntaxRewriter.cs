using System.Diagnostics;

namespace Microsoft.Language.Xml.InternalSyntax
{
    internal class SyntaxRewriter : SyntaxVisitor
    {
        public SyntaxList<TNode> VisitList<TNode>(SyntaxList<TNode> list) where TNode : GreenNode
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

        public InternalSyntax.SeparatedSyntaxList<TNode> VisitList<TNode>(InternalSyntax.SeparatedSyntaxList<TNode> list) where TNode : GreenNode
        {
            InternalSyntax.SeparatedSyntaxListBuilder<TNode> alternate = default(InternalSyntax.SeparatedSyntaxListBuilder<TNode>);
            int i = 0;
            int itemCount = list.Count;
            int separatorCount = list.SeparatorCount;
            while (i < itemCount)
            {
                var item = list[i];
                var visitedItem = this.Visit(item);

                GreenNode separator = null;
                GreenNode visitedSeparator = null;

                if (i < separatorCount)
                {
                    separator = list.GetSeparator(i);
                    var visitedSeparatorNode = this.Visit(separator);

                    Debug.Assert(visitedSeparatorNode is SyntaxToken.Green, "Cannot replace a separator with a non-separator");

                    visitedSeparator = (SyntaxToken.Green)visitedSeparatorNode;

                    Debug.Assert((separator == null &&
                        separator.Kind == SyntaxKind.None) ||
                        (visitedSeparator != null &&
                        visitedSeparator.Kind != SyntaxKind.None),
                        "Cannot delete a separator from a separated list. Removing an element will remove the corresponding separator.");
                }

                if (item != visitedItem && alternate.IsNull)
                {
                    alternate = new InternalSyntax.SeparatedSyntaxListBuilder<TNode>(itemCount);
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

        public override GreenNode VisitXmlDocument(XmlDocumentSyntax.Green node)
        {
            bool anyChanges = false;
            var newDeclaration = ((XmlDeclarationSyntax.Green)Visit(node.Prologue));
            if (node.Prologue != newDeclaration)
            {
                anyChanges = true;
            }

            var newPrecedingMisc = VisitList<GreenNode>(node.PrecedingMisc);
            if (node.PrecedingMisc != newPrecedingMisc.Node)
            {
                anyChanges = true;
            }

            var newRoot = ((XmlNodeSyntax.Green)Visit(node.Body));
            if (node.Body != newRoot)
            {
                anyChanges = true;
            }

            var newFollowingMisc = VisitList<GreenNode>(node.FollowingMisc);
            if (node.FollowingMisc != newFollowingMisc.Node)
            {
                anyChanges = true;
            }

            var newEof = VisitSyntaxToken(node.Eof);
            if (node.Eof != newEof)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return new XmlDocumentSyntax.Green(
                    newDeclaration,
                    newPrecedingMisc.Node,
                    newRoot,
                    newFollowingMisc.Node,
                    newEof);
            }
            else
            {
                return node;
            }
        }

        public override GreenNode VisitXmlDeclaration(XmlDeclarationSyntax.Green node)
        {
            bool anyChanges = false;
            var newLessThanQuestionToken = ((PunctuationSyntax.Green)Visit(node.LessThanQuestionToken));
            if (node.LessThanQuestionToken != newLessThanQuestionToken)
            {
                anyChanges = true;
            }

            var newXmlKeyword = ((KeywordSyntax.Green)Visit(node.XmlKeyword));
            if (node.XmlKeyword != newXmlKeyword)
            {
                anyChanges = true;
            }

            var newVersion = ((XmlDeclarationOptionSyntax.Green)Visit(node.Version));
            if (node.Version != newVersion)
            {
                anyChanges = true;
            }

            var newEncoding = ((XmlDeclarationOptionSyntax.Green)Visit(node.Encoding));
            if (node.Encoding != newEncoding)
            {
                anyChanges = true;
            }

            var newStandalone = ((XmlDeclarationOptionSyntax.Green)Visit(node.Standalone));
            if (node.Standalone != newStandalone)
            {
                anyChanges = true;
            }

            var newQuestionGreaterThanToken = ((PunctuationSyntax.Green)Visit(node.QuestionGreaterThanToken));
            if (node.QuestionGreaterThanToken != newQuestionGreaterThanToken)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return new XmlDeclarationSyntax.Green(
                    newLessThanQuestionToken,
                    newXmlKeyword,
                    newVersion,
                    newEncoding,
                    newStandalone,
                    newQuestionGreaterThanToken);
            }
            else
            {
                return node;
            }
        }

        public override GreenNode VisitXmlDeclarationOption(XmlDeclarationOptionSyntax.Green node)
        {
            bool anyChanges = false;
            var newName = ((XmlNameTokenSyntax.Green)Visit(node.Name));
            if (node.Name != newName)
            {
                anyChanges = true;
            }

            var newEquals = ((PunctuationSyntax.Green)Visit(node.Equals));
            if (node.Equals != newEquals)
            {
                anyChanges = true;
            }

            var newValue = ((XmlStringSyntax.Green)Visit(node.Value));
            if (node.Value != newValue)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return new XmlDeclarationOptionSyntax.Green(newName, newEquals, newValue);
            }
            else
            {
                return node;
            }
        }

        public override GreenNode VisitXmlElement(XmlElementSyntax.Green node)
        {
            bool anyChanges = false;
            var newStartTag = ((XmlElementStartTagSyntax.Green)Visit(node.StartTag));
            if (node.StartTag != newStartTag)
            {
                anyChanges = true;
            }

            var newContent = VisitList<GreenNode>(node.Content);
            if (node.Content != newContent.Node)
            {
                anyChanges = true;
            }

            var newEndTag = ((XmlElementEndTagSyntax.Green)Visit(node.EndTag));
            if (node.EndTag != newEndTag)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return new XmlElementSyntax.Green(newStartTag, newContent.Node, newEndTag);
            }
            else
            {
                return node;
            }
        }

        public override GreenNode VisitXmlText(XmlTextSyntax.Green node)
        {
            bool anyChanges = false;
            var newTextTokens = VisitList<GreenNode>(node.TextTokens);
            if (node.TextTokens != newTextTokens.Node)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return new XmlTextSyntax.Green(newTextTokens.Node);
            }
            else
            {
                return node;
            }
        }

        public override GreenNode VisitXmlElementStartTag(XmlElementStartTagSyntax.Green node)
        {
            bool anyChanges = false;
            var newLessThanToken = ((PunctuationSyntax.Green)Visit(node.LessThanToken));
            if (node.LessThanToken != newLessThanToken)
            {
                anyChanges = true;
            }

            var newName = ((XmlNameSyntax.Green)Visit(node.NameNode));
            if (node.NameNode != newName)
            {
                anyChanges = true;
            }

            var newAttributes = VisitList<GreenNode>(node.Attributes);
            if (node.Attributes != newAttributes.Node)
            {
                anyChanges = true;
            }

            var newGreaterThanToken = ((PunctuationSyntax.Green)Visit(node.GreaterThanToken));
            if (node.GreaterThanToken != newGreaterThanToken)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return new XmlElementStartTagSyntax.Green(newLessThanToken, newName, newAttributes.Node, newGreaterThanToken);
            }
            else
            {
                return node;
            }
        }

        public override GreenNode VisitXmlElementEndTag(XmlElementEndTagSyntax.Green node)
        {
            bool anyChanges = false;
            var newLessThanSlashToken = ((PunctuationSyntax.Green)Visit(node.LessThanSlashToken));
            if (node.LessThanSlashToken != newLessThanSlashToken)
            {
                anyChanges = true;
            }

            var newName = ((XmlNameSyntax.Green)Visit(node.NameNode));
            if (node.NameNode != newName)
            {
                anyChanges = true;
            }

            var newGreaterThanToken = ((PunctuationSyntax.Green)Visit(node.GreaterThanToken));
            if (node.GreaterThanToken != newGreaterThanToken)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return new XmlElementEndTagSyntax.Green(newLessThanSlashToken, newName, newGreaterThanToken);
            }
            else
            {
                return node;
            }
        }

        public override GreenNode VisitXmlEmptyElement(XmlEmptyElementSyntax.Green node)
        {
            bool anyChanges = false;
            var newLessThanToken = ((PunctuationSyntax.Green)Visit(node.LessThanToken));
            if (node.LessThanToken != newLessThanToken)
            {
                anyChanges = true;
            }

            var newName = ((XmlNameSyntax.Green)Visit(node.NameNode));
            if (node.NameNode != newName)
            {
                anyChanges = true;
            }

            var newAttributes = VisitList<GreenNode>(node.AttributesNode);
            if (node.AttributesNode != newAttributes.Node)
            {
                anyChanges = true;
            }

            var newSlashGreaterThanToken = ((PunctuationSyntax.Green)Visit(node.SlashGreaterThanToken));
            if (node.SlashGreaterThanToken != newSlashGreaterThanToken)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return new XmlEmptyElementSyntax.Green(newLessThanToken, newName, newAttributes.Node, newSlashGreaterThanToken);
            }
            else
            {
                return node;
            }
        }

        public override GreenNode VisitXmlAttribute(XmlAttributeSyntax.Green node)
        {
            bool anyChanges = false;
            var newName = ((XmlNameSyntax.Green)Visit(node.NameNode));
            if (node.NameNode != newName)
            {
                anyChanges = true;
            }

            var newEqualsToken = ((PunctuationSyntax.Green)Visit(node.Equals));
            if (node.Equals != newEqualsToken)
            {
                anyChanges = true;
            }

            var newValue = ((XmlNodeSyntax.Green)Visit(node.ValueNode));
            if (node.ValueNode != newValue)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return new XmlAttributeSyntax.Green(newName, newEqualsToken, newValue);
            }
            else
            {
                return node;
            }
        }

        public override GreenNode VisitXmlString(XmlStringSyntax.Green node)
        {
            bool anyChanges = false;
            var newStartQuoteToken = ((PunctuationSyntax.Green)Visit(node.StartQuoteToken));
            if (node.StartQuoteToken != newStartQuoteToken)
            {
                anyChanges = true;
            }

            var newTextTokens = VisitList(node.TextTokens);
            if (node.TextTokens != newTextTokens.Node)
            {
                anyChanges = true;
            }

            var newEndQuoteToken = ((PunctuationSyntax.Green)Visit(node.EndQuoteToken));
            if (node.EndQuoteToken != newEndQuoteToken)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return new XmlStringSyntax.Green(newStartQuoteToken, newTextTokens.Node, newEndQuoteToken);
            }
            else
            {
                return node;
            }
        }

        public override GreenNode VisitXmlName(XmlNameSyntax.Green node)
        {
            bool anyChanges = false;
            var newPrefix = ((XmlPrefixSyntax.Green)Visit(node.Prefix));
            if (node.Prefix != newPrefix)
            {
                anyChanges = true;
            }

            var newLocalName = ((XmlNameTokenSyntax.Green)Visit(node.LocalName));
            if (node.LocalName != newLocalName)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return new XmlNameSyntax.Green(newPrefix, newLocalName);
            }
            else
            {
                return node;
            }
        }

        //public override GreenNode VisitXmlPrefixName(XmlPrefixSyntax node)
        //{
        //    bool anyChanges = false;
        //    var newName = ((XmlNameTokenSyntax)Visit(node.Name));
        //    if (node.Name != newName)
        //    {
        //        anyChanges = true;
        //    }

        //    var newColonToken = ((PunctuationSyntax)Visit(node.ColonToken));
        //    if (node.ColonToken != newColonToken)
        //    {
        //        anyChanges = true;
        //    }

        //    if (anyChanges)
        //    {
        //        return new XmlPrefixSyntax(newName, newColonToken);
        //    }
        //    else
        //    {
        //        return node;
        //    }
        //}

        public override GreenNode VisitXmlComment(XmlCommentSyntax.Green node)
        {
            bool anyChanges = false;
            var newLessThanExclamationMinusMinusToken = ((PunctuationSyntax.Green)Visit(node.BeginComment));
            if (node.BeginComment != newLessThanExclamationMinusMinusToken)
            {
                anyChanges = true;
            }

            var newTextTokens = VisitList<GreenNode>(node.Content);
            if (node.Content != newTextTokens.Node)
            {
                anyChanges = true;
            }

            var newMinusMinusGreaterThanToken = ((PunctuationSyntax.Green)Visit(node.EndComment));
            if (node.EndComment != newMinusMinusGreaterThanToken)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return new XmlCommentSyntax.Green(newLessThanExclamationMinusMinusToken, newTextTokens.Node, newMinusMinusGreaterThanToken);
            }
            else
            {
                return node;
            }
        }

        public override GreenNode VisitXmlProcessingInstruction(XmlProcessingInstructionSyntax.Green node)
        {
            bool anyChanges = false;
            var newLessThanQuestionToken = ((PunctuationSyntax.Green)Visit(node.LessThanQuestionToken));
            if (node.LessThanQuestionToken != newLessThanQuestionToken)
            {
                anyChanges = true;
            }

            var newName = ((XmlNameTokenSyntax.Green)Visit(node.Name));
            if (node.Name != newName)
            {
                anyChanges = true;
            }

            var newTextTokens = VisitList<GreenNode>(node.TextTokens);
            if (node.TextTokens != newTextTokens.Node)
            {
                anyChanges = true;
            }

            var newQuestionGreaterThanToken = ((PunctuationSyntax.Green)Visit(node.QuestionGreaterThanToken));
            if (node.QuestionGreaterThanToken != newQuestionGreaterThanToken)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return new XmlProcessingInstructionSyntax.Green(newLessThanQuestionToken, newName, newTextTokens.Node, newQuestionGreaterThanToken);
            }
            else
            {
                return node;
            }
        }

        public override GreenNode VisitXmlCDataSection(XmlCDataSectionSyntax.Green node)
        {
            bool anyChanges = false;
            var newBeginCDataToken = ((PunctuationSyntax.Green)Visit(node.BeginCData));
            if (node.BeginCData != newBeginCDataToken)
            {
                anyChanges = true;
            }

            var newTextTokens = VisitList<GreenNode>(node.TextTokens);
            if (node.TextTokens != newTextTokens.Node)
            {
                anyChanges = true;
            }

            var newEndCDataToken = ((PunctuationSyntax.Green)Visit(node.EndCData));
            if (node.EndCData != newEndCDataToken)
            {
                anyChanges = true;
            }

            if (anyChanges)
            {
                return new XmlCDataSectionSyntax.Green(newBeginCDataToken, newTextTokens.Node, newEndCDataToken);
            }
            else
            {
                return node;
            }
        }
    }
}
