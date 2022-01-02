using System.Xml.Serialization;

namespace PrevueCommander.XmlTv.Model;

[XmlRoot("tv")]
public class Tv
{
    [XmlElement(ElementName="channel")]
    public List<Channel> Channel { get; set; }
    [XmlElement(ElementName="programme")]
    public List<Programme> Programme { get; set; }
    [XmlAttribute(AttributeName="source-info-url")]
    public string Sourceinfourl { get; set; }
    [XmlAttribute(AttributeName="source-info-name")]
    public string Sourceinfoname { get; set; }
    [XmlAttribute(AttributeName="generator-info-name")]
    public string Generatorinfoname { get; set; }
    [XmlAttribute(AttributeName="generator-info-url")]
    public string Generatorinfourl { get; set; }
}
