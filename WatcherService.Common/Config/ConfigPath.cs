using System;
using System.Xml.Serialization;

namespace WatcherService.Common.Config
{
    [Serializable]
    public class ConfigPath
    {
        [XmlAttribute("path")]
        public string Path { get; set; }
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlElement("description")]
        public string Description { get; set; }
        [XmlAttribute("includeSubDirectories")]
        public bool IncludeSubDirectories { get; set; }
        [XmlText]
        public string Script { get; set; }
        [XmlAttribute("action")]
        public ActionOnPath Action { get; set; }
    }
    public enum ActionOnPath
    {
        Create,
        Delete,
        Update,
        Rename
    }
}
