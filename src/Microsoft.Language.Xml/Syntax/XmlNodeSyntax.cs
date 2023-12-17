using System;
using System.Collections.Generic;

namespace Microsoft.Language.Xml
{
    public abstract class XmlNodeSyntax : SyntaxNode
    {
        internal abstract class Green : InternalSyntax.GreenNode
        {
            protected Green(SyntaxKind kind)
                : base(kind)
            {
            }

            protected Green(SyntaxKind kind, int fullWidth)
                : base(kind, fullWidth)
            {
            }

            protected Green(SyntaxKind kind, DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations)
                : base(kind, diagnostics, annotations)
            {
            }

            internal override InternalSyntax.GreenNode Accept(InternalSyntax.SyntaxVisitor visitor)
            {
                return visitor.VisitXmlNode(this);
            }
        }

        internal new Green GreenNode => (Green)base.GreenNode;

        internal XmlNodeSyntax(Green green, SyntaxNode parent, int position) : base(green, parent, position)
        {
        }

        public override SyntaxNode Accept(SyntaxVisitor visitor)
        {
            return visitor.VisitXmlNode(this);
        }
    }
}
