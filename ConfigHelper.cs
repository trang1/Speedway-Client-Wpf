using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Xml;
using SpeedwayClientWpf.ViewModels;

namespace SpeedwayClientWpf
{
    /// <summary>
    /// Provides operations with app.config file.
    /// </summary>
    public class ConfigHelper
    {
        private const string AppSettingsNodePath = "//appSettings";
        private const string ReadersNodePath = "//speedwayclientwpf.readers";
        private static readonly XmlDocument Settings;
        private static readonly XmlNode AppSettingsSection;
        private static readonly XmlNode ReadersSection;

        static ConfigHelper()
        {
            using (var stream = new FileStream(Path, FileMode.OpenOrCreate))
            {
                Settings = new XmlDocument();
                try
                {
                    Settings.Load(stream);
                    AppSettingsSection = Settings.SelectSingleNode(AppSettingsNodePath);
                    ReadersSection = Settings.SelectSingleNode(ReadersNodePath);
                }
                catch (XmlException ex)
                {
                    Trace.TraceError("Error in ConfigHelper initialization. " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Gets the path of the app.config file
        /// </summary>
        private static string Path
        {
            get { return AppDomain.CurrentDomain.SetupInformation.ConfigurationFile; }
        }

        #region public methods

        /// <summary>
        /// Gets property value from appSettings by key
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>Property value</returns>
        public static string Get(string key)
        {
            if (AppSettingsSection == null)
                return string.Empty;
            var xmlElement = AppSettingsSection.SelectSingleNode(string.Format("//add[@key='{0}']", key)) as XmlElement;
            return xmlElement == null ? string.Empty : xmlElement.GetAttribute("value");
        }

        /// <summary>
        /// Saves property value into appSettings
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">Property value</param>
        public static void Set(string key, string value)
        {
            if (AppSettingsSection == null)
                return;

            var xmlElement = (XmlElement) AppSettingsSection.SelectSingleNode(string.Format("//add[@key='{0}']", key));
            if (xmlElement == null)
            {
                // creating new element
                xmlElement = Settings.CreateElement("add");
                xmlElement.SetAttribute("key", key);
                xmlElement.SetAttribute("value", value);
                AppSettingsSection.AppendChild(xmlElement);
            }
            else
            {
                // updating existing element
                xmlElement.SetAttribute("value", value);
            }
            Settings.Save(Path);
        }

        /// <summary>
        /// Gets readers info from readers section of the config file
        /// </summary>
        /// <returns>List of readers</returns>
        public static IList<ReaderViewModel> GetReaders()
        {
            var list = new List<ReaderViewModel>();
            if (ReadersSection != null)
            {
                foreach (object childNode in ReadersSection.ChildNodes)
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
                        string error = "ERROR getting readers settings from config. " + exception.Message;
                        MainWindowViewModel.Instance.PushMessage(new LogMessage(LogMessageType.Error, error));
                        Trace.TraceError(error + exception.StackTrace);
                    }
                }
            }
            // if number of readers is less than 4, add empty items to fill the list 
            while (list.Count < 4)
                list.Add(new ReaderViewModel {Name = "Reader " + (list.Count + 1), Port = "14150"});

            return list;
        }

        /// <summary>
        /// Saves readers info into readers section of the config file
        /// </summary>
        /// <param name="readers">List of readers</param>
        public static void SaveReaders(IList<ReaderViewModel> readers)
        {
            if (ReadersSection == null) return;

            foreach (ReaderViewModel reader in readers)
            {
                var xmlElement =
                    ReadersSection.SelectSingleNode(string.Format("//reader[@name='{0}']", reader.Name)) as XmlElement;
                if (xmlElement != null)
                {
                    xmlElement.SetAttribute("ipaddress", reader.IpAddress);
                    xmlElement.SetAttribute("port", reader.Port);
                }
            }
            Settings.Save(Path);
        }
        #endregion
    }

    /// <summary>
    /// Represents our config section in app.config file
    /// </summary>
    public class ReadersConfigurationSection : ConfigurationSection
    {
    }
}