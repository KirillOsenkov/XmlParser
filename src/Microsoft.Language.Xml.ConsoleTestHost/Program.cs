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
    <n:T></n:T>
    <X/>
    <A.B></A.B>
    <A B=""""></A>
    <A>&#x03C0;</A>
    <A>a &lt;</A>
    <A><![CDATA[bar]]></A>
    <!-- comment -->
</X>";
            var root = Parser.ParseText(text);
			Console.WriteLine (root.ToFullString ());
			Console.WriteLine (text.Length);
			root.Accept (new PositionVisitor ());
            Console.ReadKey();
        }
    }

	class PositionVisitor : SyntaxRewriter
	{
		public override SyntaxNode VisitSyntaxNode(SyntaxNode node)
		{
			Console.WriteLine ("{0} => {1}", node.GetType ().Name, node.Span.ToString ());
			return node;
		}

		public override SyntaxToken VisitSyntaxToken(SyntaxToken token)
		{
			Console.WriteLine ("{0}: {2} => {1}", token.GetType ().Name, token.Span.ToString (), token.Text);
			return base.VisitSyntaxToken(token);
		}
	}
}
