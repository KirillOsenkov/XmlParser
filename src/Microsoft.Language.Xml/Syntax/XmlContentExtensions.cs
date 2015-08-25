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

        internal static int MatchEndElement(this List<XmlContext> @this, XmlNameSyntax name)
        {
            Debug.Assert(name == null || name.Kind == SyntaxKind.XmlName);
            var last = @this.Count - 1;
            if (name == null)
            {
                return last;
            }

            var i = last;
            while (i >= 0)
            {
                var context = @this[i];
                var nameExpr = context.StartElement.NameNode;
                if (nameExpr.Kind == SyntaxKind.XmlName)
                {
                    var startName = ((XmlNameSyntax)nameExpr);
                    if (startName.LocalName.Text == name.LocalName.Text)
                    {
                        var startPrefix = startName.Prefix;
                        var endPrefix = name.Prefix;
                        if (startPrefix == endPrefix)
                        {
                            break;
                        }

                        if (startPrefix != null && endPrefix != null)
                        {
                            if (startPrefix.Name.Text == endPrefix.Name.Text)
                            {
                                break;
                            }
                        }
                    }
                }

                i -= 1;
            }

            return i;
        }
    }
}
