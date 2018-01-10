using System;
using System.IO;

namespace Microsoft.Language.Xml.ConsoleTestHost
{
    class Program
    {
        static void Main(string[] args)
        {
            //var text = File.ReadAllText(@"D:\1.xml");
            var text =
@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<X>
	<!-- Everything that is a Y node should be reused -->
	<Y>Unrelated node</Y>
    <foo attributeName=""attributeValue"">
		<Y>&amp;</Y>
	</foo>
</X>";
            var root = Parser.ParseText(text);
            Console.WriteLine(root.ToFullString());
            Console.WriteLine(text.Length);
            root.Accept(new PositionVisitor());

            // Now reparse the tree
            var newBuffer = new StringBuffer(text.Replace("attributeValue", "newAttributeValue"));
            root = Parser.ParseIncremental(newBuffer, new[] { new TextChangeRange(new TextSpan(166, "attributeValue".Length), "newAttributeValue".Length) }, root);
            Console.WriteLine(root.ToFullString());

            Console.ReadKey();
        }
    }

    class PositionVisitor : SyntaxRewriter
    {
        public override SyntaxNode VisitSyntaxNode(SyntaxNode node)
        {
            Console.WriteLine("{0} => {1}", node.GetType().Name, node.Span.ToString());
            return node;
        }

        public override SyntaxToken VisitSyntaxToken(SyntaxToken token)
        {
            Console.WriteLine("{0}: {2} => {1}", token.GetType().Name, token.Span.ToString(), token.Text);
            return base.VisitSyntaxToken(token);
        }
    }
}
