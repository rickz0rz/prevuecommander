using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace PrevueCommander.XmlTv.Model;

[XmlRoot(ElementName = "channel")]
public class Channel
{
    [XmlElement(ElementName = "display-name")]
    public List<string> Displayname { get; set; }

    [XmlElement(ElementName = "icon")] public Icon Icon { get; set; }
    [XmlAttribute(AttributeName = "id")] public string Id { get; set; }

    public Channel()
    {
        _callSign = new Lazy<string>(new Func<string>(() =>
        {
            foreach (var component in from displayName in Displayname
                     select displayName.Split(" ")
                     into components
                     from component in components
                     where component.ToCharArray().Any(char.IsLetter) && !component.Contains(':')
                     select component)
            {
                return component;
            }

            throw new Exception("Unable to find channel number in displayName");
        }));

        _channelNumber = new Lazy<string>(() =>
        {
            foreach (var component in from displayName in Displayname
                     select displayName.Split(" ")
                     into components
                     from component in components
                     where component.ToCharArray().All(c => char.IsDigit(c) || c is '.' or '-')
                     select component)
            {
                return component;
            }

            throw new Exception("Unable to find channel number in displayName");
        });

        _sourceName = new Lazy<string>(() =>
        {
            var shaM = SHA512.Create();
            var hashString = Convert.ToHexString(shaM.ComputeHash(Encoding.ASCII.GetBytes(Id)));
            return hashString.Length > 6 ? hashString[..6] : hashString;
        });
    }

    private Lazy<string> _callSign;
    private Lazy<string> _channelNumber;
    private Lazy<string> _sourceName;

    public string CallSign => _callSign.Value;
    public string ChannelNumber => _channelNumber.Value;
    public string SourceName => _sourceName.Value;
}
