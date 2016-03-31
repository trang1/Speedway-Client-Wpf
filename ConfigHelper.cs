using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SpeedwayClientWpf.ViewModels;

namespace SpeedwayClientWpf
{
    public class ConfigHelper
    {
        //private const string ApplicationName = "Cun5.exe";
        private static string Path
        {
            get { return AppDomain.CurrentDomain.SetupInformation.ConfigurationFile; }
        }

        private static readonly XmlDocument Settings;
        private static readonly XmlNode SelectSingleNode;
        private static readonly XmlNode ReadersSection;
        private const string Xpath = "//add[@key='{0}']";
        private const string AppSettingsNode = "//appSettings";
        private const string ReadersNode = "//speedwayclientwpf.readers";

        static ConfigHelper()
        {
            using (var stream = new FileStream(Path, FileMode.OpenOrCreate))
            {
                Settings = new XmlDocument();
                try
                {
                    Settings.Load(stream);
                    SelectSingleNode = Settings.SelectSingleNode(AppSettingsNode);
                    ReadersSection = Settings.SelectSingleNode(ReadersNode);
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
            var xmlElement = (XmlElement) SelectSingleNode.SelectSingleNode(string.Format(Xpath, key));
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
            var xmlElement = SelectSingleNode.SelectSingleNode(string.Format(Xpath, key)) as XmlElement;
            return xmlElement == null ? string.Empty : xmlElement.GetAttribute("value");
        }

        public static IList<ReaderViewModel> GetReaders()
        {
            var list = new List<ReaderViewModel>();
            if (ReadersSection != null)
            {
                foreach (var childNode in ReadersSection.ChildNodes)
                {
                    try
                    {
                        var node = childNode as XmlElement;
                        if (node == null) continue;

                        var rvm = new ReaderViewModel
                        {
                            Name = node.GetAttribute("name"),
                            IpAddress = node.GetAttribute("ipaddress"),
                            Port = node.GetAttribute("port")
                        };
                        list.Add(rvm);
                    }
                    catch (Exception exception)
                    {
                        var error = "ERROR getting readers settings from config. " + exception.Message;

                        MainWindowViewModel.Instance.PushMessage(new LogMessage(LogMessageType.Error, error));
                        Trace.TraceError(error + exception.StackTrace);
                    }
                }
            }

            while (list.Count < 4)
                list.Add(new ReaderViewModel {Name = "Reader " + (list.Count + 1), Port = "14150"});

            return list;
        }

        public static void SaveReaders(IList<ReaderViewModel> readers)
        {
            if(ReadersSection == null) return;

            foreach (var reader in readers)
            {
                var xmlElement = ReadersSection.SelectSingleNode(string.Format("//reader[@name='{0}']", reader.Name)) as XmlElement;
                if (xmlElement != null)
                {
                    xmlElement.SetAttribute("ipaddress", reader.IpAddress);
                    xmlElement.SetAttribute("port", reader.Port);
                }
            }
            Settings.Save(Path);
        }
    }
    public class ReadersConfigurationSection : ConfigurationSection
    {
        
    }
}
