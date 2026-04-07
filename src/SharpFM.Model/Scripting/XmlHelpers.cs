using System;
using System.Text;
using System.Xml;
using System.Xml.Linq;
namespace SharpFM.Model.Scripting;

public static class XmlHelpers
{
    public static string XmlEscape(string s)
    {
        return s.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
    }

    public static string Unquote(string s)
    {
        if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
            return s[1..^1];
        return s;
    }

    public static string PrettyPrint(string xml)
    {
        try
        {
            var element = XElement.Parse(xml);
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                NewLineOnAttributes = false
            };
            using (var writer = XmlWriter.Create(sb, settings))
            {
                element.Save(writer);
            }
            return sb.ToString();
        }
        catch (Exception)
        {
            return xml;
        }
    }
}
