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
            { "Mac-XMTB", "Table" },
            { "Mac-XMFD", "Field" },
            { "Mac-XMSC", "Script" }
        };

        /// <summary>
        /// Constructor taking in the raw data byte array.
        /// </summary>
        /// <param name="name">The name of the clip.</param>
        /// <param name="format">Format of the clip.</param>
        /// <param name="data">Data containing the clip.</param>
        public FileMakerClip(string name, string format, byte[] data)
        {
            // pull in the name
            Name = name;
            // load the format
            ClipboardFormat = format;
            // skip the first four bytes, as this is a length check
            XmlData = ClipBytesToPrettyXml(data.Skip(4));

            // if the data is empty, move on.
            if (string.IsNullOrEmpty(XmlData)) { return; }

            // try to show better "name" if possible
            var xdoc = XDocument.Load(new StringReader(XmlData));
            var containerName = xdoc.Element("fmxmlsnippet")?.Descendants().First()?.Attribute("name")?.Value;
            if (!string.IsNullOrEmpty(containerName))
            {
                Name = containerName;
            }
        }

        /// <summary>
        /// Clipboard Format
        /// </summary>
        public string ClipboardFormat { get; set; }

        /// <summary>
        /// Name of Clip
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Raw data that can be put back onto the Clipboard in FileMaker structure.
        /// </summary>
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

        /// <summary>
        /// The actual clip. Users work with the Xml version here, and then pull the RawData property when ready to write back to FileMaker.
        /// </summary>
        public string XmlData { get; set; }

        /// <summary>
        /// Utility method for prettifying the Xml for a user to read.
        /// </summary>
        /// <param name="clipData">The byte array containing the xml data.</param>
        /// <returns>A prettified version of the byte array as a formatted xml string.</returns>
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

        /// <summary>
        /// Make an Xml string pretty.
        /// </summary>
        /// <param name="xml">Raw xml to make pretty.</param>
        /// <returns>A pretty (human readable) version of the input string.</returns>
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