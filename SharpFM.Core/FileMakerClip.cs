using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SharpFM.Core
{
    public class FileMakerClip
    {
        public static Dictionary<string, string> ClipTypes { get; set; } = new Dictionary<string, string>
        {
            { "Mac-XMSS", "ScriptSteps" },
            { "Mac-XML2", "Layout" },
            { "Mac-XMTD", "Table" },
            { "Mac-XMFD", "Field" },
            { "Mac-XMSC", "Script" }
        };

        public FileMakerClip(string name, string format, byte[] data)
        {
            Name = name;
            ClipboardFormat = format;
            XmlData = ClipBytesToPrettyXml(data.Skip(4));

            if (string.IsNullOrEmpty(XmlData)) { return; }

            // try to show better "name" if possible
            var xdoc = XDocument.Load(new StringReader(XmlData));
            var containerName = xdoc.Element("fmxmlsnippet")?.Descendants().First()?.Attribute("name")?.Value;
            if (!string.IsNullOrEmpty(containerName))
            {
                Name = containerName;
            }
        }

        public string ClipboardFormat { get; set; }

        public string Name { get; set; }

        public byte[] RawData
        {
            get
            {
                // recalculate the length of the original text and make sure that is the first four bytes in the stream
                byte[] byteList = Encoding.UTF8.GetBytes(XmlData);
                int bl = byteList.Length;
                byte[] intBytes = BitConverter.GetBytes(bl);
                return intBytes.Concat(byteList).ToArray();
            }
        }

        public string XmlData { get; set; }


        public static string ClipBytesToPrettyXml(IEnumerable<byte> clipData)
        {
            var xmlComments = Encoding.UTF8.GetString(clipData.ToArray());
            if (string.IsNullOrEmpty(xmlComments))
            {
                // dont try to prettify if we don't have content
                return xmlComments;
            }
            return PrettyXml(xmlComments);
        }

        private static string PrettyXml(string xml)
        {
            var stringBuilder = new StringBuilder();

            var element = XElement.Parse(xml);

            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                NewLineOnAttributes = false
            };

            using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
            {
                element.Save(xmlWriter);
            }

            return stringBuilder.ToString();
        }
    }
}