using System;
using System.IO;

namespace Microsoft.Language.Xml.ConsoleTestHost
{
    class Program
    {
        static void Main(string[] args)
        {
            var text = File.ReadAllText(@"D:\1.xml");
            var root = Parser.ParseText(text);
            Console.ReadKey();
        }
    }
}
