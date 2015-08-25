using System.Collections.Generic;

namespace Microsoft.Language.Xml
{
    public interface IXmlElement
    {
        string Name { get; }
        IXmlElement Parent { get; }
        IEnumerable<IXmlElement> Elements { get; }
        IEnumerable<KeyValuePair<string, string>> Attributes { get; }
        string this[string attributeName] { get; }
    }
}
