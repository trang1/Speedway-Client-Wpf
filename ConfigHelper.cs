using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SpeedwayClientWpf
{
    public class ConfigHelper
    {
        //private const string ApplicationName = "Cun5.exe";
        private static string Path { get { return AppDomain.CurrentDomain.SetupInformation.ConfigurationFile; } }
        private static readonly XmlDocument Settings;
        private static readonly XmlNode SelectSingleNode;
        private const string Xpath = "//add[@key='{0}']";
        private const string SelectNode = "//appSettings";
        static ConfigHelper()
        {
            using (var stream = new FileStream(Path, FileMode.OpenOrCreate))
            {
                Settings = new XmlDocument();
                try
                {
                    Settings.Load(stream);
                    SelectSingleNode = Settings.SelectSingleNode(SelectNode);
                }
                catch (XmlException ex)
                {
                    Trace.TraceError("Error in ConfigHelper initialization. " + ex.Message);
                }
            }
        }
        public static void Set(string key, string value)
        {
            if (SelectSingleNode == null)
                return;
            var xmlElement = (XmlElement)SelectSingleNode.SelectSingleNode(string.Format(Xpath, key));
            if (xmlElement == null)
            {
                xmlElement = Settings.CreateElement("add");
                xmlElement.SetAttribute("key", key);
                xmlElement.SetAttribute("value", value);
                SelectSingleNode.AppendChild(xmlElement);
            }
            else
            {
                xmlElement.SetAttribute("value", value);
            }
            Settings.Save(Path);
        }
        public static string Get(string key)
        {
            if (SelectSingleNode == null)
                return string.Empty;
            var xmlElement = (XmlElement)SelectSingleNode.SelectSingleNode(string.Format(Xpath, key));
            return xmlElement == null ? string.Empty : xmlElement.GetAttribute("value");
        }
    }
}
