using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace ZipStudio.Core
{
    public class Manifest
    {
        protected XDocument xmlDocument;
        protected XmlSchemaSet xmlSchemaSet;
        protected UTF8Encoding utfEncoding = new UTF8Encoding(true);

        #region Properties

        protected string GetContents(string xpath)
        {
            return xmlDocument.Root?.Element(xpath)?.Value;
        }

        protected void SetContents(string xpath, string value)
        {
            XElement element = xmlDocument.Root?.Element(xpath);

            if (element == null)
            {
                xmlDocument.Root.Add(new XElement(value));
            }
            else
            {
                element.Value = value;
            }
        }

        public string Guid
        {
            get => GetContents("guid");
            set => SetContents("guid", value);
        }

        public string Name
        {
            get => GetContents("name");
            set => SetContents("name", value);
        }

        public string Version
        {
            get => GetContents("version");
            set => SetContents("version", value);
        }

        public string Author
        {
            get => GetContents("author");
            set => SetContents("author", value);
        }

        public string Description
        {
            get => GetContents("description");
            set => SetContents("description", value);
        }

        public string Website
        {
            get => GetContents("website");
            set => SetContents("website", value);
        }

        public string Game
        {
            get => GetContents("game");
            set => SetContents("game", value);
        }

        #endregion

        public Manifest()
        {
            xmlDocument = XDocument.Parse(Properties.Resources.manifest_template);

            XmlSchemaSet schemas = new XmlSchemaSet();
            schemas.Add("", XmlReader.Create(new StringReader(Properties.Resources.manifest_schema)));
        }

        public Manifest(string xml)
        {
            xmlDocument = XDocument.Parse(xml);
        }

        public Manifest(Stream stream)
        {
            xmlDocument = XDocument.Load(stream);
        }

        public bool Verify()
        {
            bool success = true;
            xmlDocument.Validate(xmlSchemaSet, (o, e) => success = false);

            return success;
        }

        public string Export()
        {
            return xmlDocument.ToString();
        }

        public byte[] ExportAsBytes()
        {
            return utfEncoding.GetBytes(Export());
        }

        public static bool TryParseManifest(Stream stream, out Manifest manifest)
        {
            manifest = new Manifest(stream);

            if (!manifest.Verify())
            {
                manifest = null;
                return false;
            }

            return true;
        }

        public static bool TryParseManifest(string xml, out Manifest manifest)
        {
            manifest = new Manifest(xml);

            if (!manifest.Verify())
            {
                manifest = null;
                return false;
            }

            return true;
        }
    }
}
