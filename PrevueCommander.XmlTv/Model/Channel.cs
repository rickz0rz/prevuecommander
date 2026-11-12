using System.Xml.Serialization;

namespace PrevueCommander.XmlTv.Model;

[XmlRoot(ElementName = "channel")]
public class Channel
{
    [XmlElement(ElementName = "display-name")]
    public List<string>? DisplayName { get; set; }

    [XmlElement(ElementName = "icon")] public Icon? Icon { get; set; }

    [XmlAttribute(AttributeName = "id")] public string? Id { get; set; }

    /// <summary>The first word of a display name that has a letter and no ':', for example "WTVS" in "6 WTVS".</summary>
    public string CallSign => Words().FirstOrDefault(word => word.Any(char.IsLetter) && !word.Contains(':')) ??
                              string.Empty;

    /// <summary>The first word of a display name that is a number, for example "6.1" in "6.1 WTVS".</summary>
    public string ChannelNumber =>
        Words().FirstOrDefault(word => word.All(c => char.IsDigit(c) || c is '.' or '-')) ?? string.Empty;

    private IEnumerable<string> Words() =>
        (DisplayName ?? []).SelectMany(name => name.Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
