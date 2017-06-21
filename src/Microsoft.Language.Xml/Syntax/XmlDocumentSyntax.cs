using System;
using System.Collections.Generic;

namespace Microsoft.Language.Xml
{
    public class XmlDocumentSyntax : XmlNodeSyntax, IXmlElement
    {
        public XmlNodeSyntax Body { get; private set; }
        public SyntaxNode PrecedingMisc { get; private set; }
        public SyntaxNode FollowingMisc { get; private set; }
        public XmlDeclarationSyntax Prologue { get; private set; }
        public SyntaxToken Eof { get; set; }

        public XmlDocumentSyntax(
            SyntaxKind kind,
            XmlDeclarationSyntax prologue,
            SyntaxNode precedingMisc,
            XmlNodeSyntax body,
            SyntaxNode followingMisc,
            SyntaxToken eof) : base(kind)
        {
            this.Prologue = prologue;
            this.PrecedingMisc = precedingMisc;
            this.Body = body;
            this.FollowingMisc = followingMisc;
            this.Eof = eof;
            SlotCount = 5;
        }

        public IXmlElement Root
        {
            get
            {
                return Body as IXmlElement;
            }
        }

        public IXmlElementSyntax RootSyntax
        {
            get
            {
                return Body as IXmlElementSyntax;
            }
        }

        public string Name
        {
            get
            {
                if (Root == null)
                {
                    return null;
                }

                return Root.Name;
            }
        }

        IXmlElement IXmlElement.Parent
        {
            get
            {
                return null;
            }
        }

        public IEnumerable<IXmlElement> Elements
        {
            get
            {
                if (Root == null)
                {
                    return null;
                }

                return Root.Elements;
            }
        }

        public IEnumerable<KeyValuePair<string, string>> Attributes
        {
            get
            {
                if (Root == null)
                {
                    return null;
                }

                return Root.Attributes;
            }
        }

        public string Value
        {
            get
            {
                if (Root == null)
                {
                    return null;
                }

                return Root.Value;
            }
        }

        public IXmlElementSyntax AsSyntaxElement
        {
            get
            {
                return Root as IXmlElementSyntax;
            }
        }

        public string this[string attributeName]
        {
            get
            {
                if (Root == null)
                {
                    return null;
                }

                return Root[attributeName];
            }
        }

        public override SyntaxNode GetSlot(int index)
        {
            switch (index)
            {
                case 0:
                    return Prologue;
                case 1:
                    return PrecedingMisc;
                case 2:
                    return Body;
                case 3:
                    return FollowingMisc;
                case 4:
                    return Eof;
                default:
                    throw null;
            }
        }

        public override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlDocument(this);
        }
    }
}
