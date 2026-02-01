using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Language.Xml
{
    public static class XmlContextExtensions
    {
        internal static void Push(this List<XmlContext> @this, XmlContext context)
        {
            @this.Add(context);
        }

        internal static XmlContext Pop(this List<XmlContext> @this)
        {
            var last = @this.Count - 1;
            var context = @this[last];
            @this.RemoveAt(last);
            return context;
        }

        internal static XmlContext Peek(this List<XmlContext> @this, int i = 0)
        {
            var last = @this.Count - 1;
            return @this[last - i];
        }

        internal static int MatchEndElement(this List<XmlContext> @this, XmlNameSyntax.Green name, int endTagIndent = -1)
        {
            Debug.Assert(name == null || name.Kind == SyntaxKind.XmlName);
            var last = @this.Count - 1;
            if (name == null)
            {
                return last;
            }

            // Find all matching candidates by name
            int firstMatch = -1;
            int indentMatch = -1;
            var i = last;
            while (i >= 0)
            {
                var context = @this[i];
                var nameExpr = context.StartElement.NameNode;
                if (nameExpr.Kind == SyntaxKind.XmlName)
                {
                    var startName = ((XmlNameSyntax.Green)nameExpr);
                    if (startName.LocalName.Text == name.LocalName.Text)
                    {
                        var startPrefix = startName.Prefix;
                        var endPrefix = name.Prefix;
                        bool prefixMatch = startPrefix == endPrefix ||
                            (startPrefix != null && endPrefix != null &&
                             startPrefix.Name.Text == endPrefix.Name.Text);

                        if (prefixMatch)
                        {
                            if (firstMatch < 0)
                            {
                                firstMatch = i;
                            }

                            // If indent info is available and matches, prefer this candidate
                            if (endTagIndent >= 0 && context.Indent >= 0 && context.Indent == endTagIndent)
                            {
                                indentMatch = i;
                                break;
                            }
                        }
                    }
                }

                i -= 1;
            }

            // Prefer indent-based match when available, otherwise fall back to
            // the innermost name match (original behavior)
            if (indentMatch >= 0)
            {
                return indentMatch;
            }

            return firstMatch;
        }
    }
}
