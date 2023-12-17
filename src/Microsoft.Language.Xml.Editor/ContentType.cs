using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.Language.Xml
{
    public class ContentType
    {
        public const string Xml = "xml";

        [Export]
        [Name(Xml)]
        [BaseDefinition("text")]
        public static readonly ContentTypeDefinition XmlContentTypeDefinition = null;

        [Export]
        [FileExtension(".xml")]
        [ContentType(Xml)]
        internal static readonly FileExtensionToContentTypeDefinition XmlFileExtension = null;

        [Export]
        [FileExtension(".csproj")]
        [ContentType(Xml)]
        internal static readonly FileExtensionToContentTypeDefinition CsprojFileExtension = null;

        [Export]
        [FileExtension(".vbproj")]
        [ContentType(Xml)]
        internal static readonly FileExtensionToContentTypeDefinition VbprojFileExtension = null;

        [Export]
        [FileExtension(".nuspec")]
        [ContentType(Xml)]
        internal static readonly FileExtensionToContentTypeDefinition NuspecFileExtension = null;

        [Export]
        [FileExtension(".props")]
        [ContentType(Xml)]
        internal static readonly FileExtensionToContentTypeDefinition PropsFileExtension = null;

        [Export]
        [FileExtension(".targets")]
        [ContentType(Xml)]
        internal static readonly FileExtensionToContentTypeDefinition TargetsFileExtension = null;

        [Export]
        [FileExtension(".settings")]
        [ContentType(Xml)]
        internal static readonly FileExtensionToContentTypeDefinition SettingsFileExtension = null;

        [Export]
        [FileExtension(".proj")]
        [ContentType(Xml)]
        internal static readonly FileExtensionToContentTypeDefinition ProjFileExtension = null;

        [Export]
        [FileExtension(".config")]
        [ContentType(Xml)]
        internal static readonly FileExtensionToContentTypeDefinition ConfigFileExtension = null;

        [Export]
        [FileExtension(".vsixmanifest")]
        [ContentType(Xml)]
        internal static readonly FileExtensionToContentTypeDefinition VsixmanifestFileExtension = null;

        [Export]
        [FileExtension(".dgml")]
        [ContentType(Xml)]
        internal static readonly FileExtensionToContentTypeDefinition DgmlFileExtension = null;

        [Export]
        [FileExtension(".xaml")]
        [ContentType(Xml)]
        internal static readonly FileExtensionToContentTypeDefinition XamlFileExtension = null;

        [Export]
        [FileExtension(".resx")]
        [ContentType(Xml)]
        internal static readonly FileExtensionToContentTypeDefinition ResxFileExtension = null;
    }
}