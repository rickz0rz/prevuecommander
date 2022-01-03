using System.Xml.Serialization;

namespace PrevueCommander.XmlTv.Model;

[XmlRoot(ElementName="programme")]
public class Programme {
    [XmlElement(ElementName="title")]
    public List<Title> Title { get; set; }
    [XmlElement(ElementName="desc")]
    public List<Desc> Desc { get; set; }
    [XmlElement(ElementName="date")]
    public string Date { get; set; }
    [XmlElement(ElementName="category")]
    public List<Category> Category { get; set; }
    [XmlElement(ElementName="episode-num")]
    public List<EpisodeNum> EpisodeNum { get; set; }
    [XmlElement(ElementName="audio")]
    public Audio Audio { get; set; }
    [XmlElement(ElementName="previously-shown")]
    public PreviouslyShown PreviouslyShown { get; set; }
    [XmlElement(ElementName="subtitles")]
    public List<Subtitles> Subtitles { get; set; }
    [XmlAttribute(AttributeName="start")]
    public string Start { get; set; }
    [XmlAttribute(AttributeName="stop")]
    public string Stop { get; set; }
    [XmlAttribute(AttributeName="channel")]
    public string Channel { get; set; }
}
