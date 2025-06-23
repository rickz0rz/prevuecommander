using System.Xml.Serialization;

namespace PrevueCommander.XmlTv.Model;

[XmlRoot(ElementName = "category")]
public class Category
{
    [XmlAttribute(AttributeName = "lang")] public string? Lang { get; set; }

    [XmlText] public string? Text { get; set; }
}