using System.Xml.Serialization;

namespace PrevueCommander.XmlTv.Model;

[XmlRoot(ElementName = "episode-num")]
public class EpisodeNum
{
    [XmlAttribute(AttributeName = "system")]
    public string? System { get; set; }

    [XmlText]
    public string? Text { get; set; }
}
