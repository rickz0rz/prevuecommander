using System.Xml.Serialization;

namespace PrevueCommander.XmlTv.Model;

[XmlRoot(ElementName = "icon")]
public class Icon
{
    [XmlAttribute(AttributeName = "src")] public string Src { get; set; }
}
