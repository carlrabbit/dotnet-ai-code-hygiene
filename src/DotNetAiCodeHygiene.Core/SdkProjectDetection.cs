using System.Xml.Linq;

namespace DotNetAiCodeHygiene.Core;

internal static class SdkProjectDetection
{
    internal static bool IsSdkStyle(string path)
    {
        try { return IsSdkStyle(XDocument.Load(path)); }
        catch (System.Xml.XmlException) { return false; }
    }

    internal static bool IsSdkStyle(XDocument document)
    {
        XElement? root = document.Root;
        return root?.Attribute("Sdk") is not null || root?.Elements().Any(element => element.Name.LocalName == "Sdk") == true;
    }
}
