using System.Xml.Serialization;

namespace PrevueCommander.XmlTv.Model;

[XmlRoot(ElementName = "channel")]
public class Channel
{
    [XmlElement(ElementName = "display-name")]
    public List<string> Displayname { get; set; }

    [XmlElement(ElementName = "icon")] public Icon Icon { get; set; }
    [XmlAttribute(AttributeName = "id")] public string Id { get; set; }
}
