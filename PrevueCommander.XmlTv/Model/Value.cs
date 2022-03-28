using System.Xml.Serialization;

namespace PrevueCommander.XmlTv.Model;

public class Value
{
    [XmlText] public string Text { get; set; }
}
