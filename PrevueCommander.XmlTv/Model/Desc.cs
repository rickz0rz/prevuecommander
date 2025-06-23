using System.Xml.Serialization;

namespace PrevueCommander.XmlTv.Model;

[XmlRoot(ElementName = "desc")]
public class Desc
{
    [XmlAttribute(AttributeName = "lang")] public string? Lang { get; set; }

    [XmlText] public string? Text { get; set; }
}