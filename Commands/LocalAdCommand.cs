using System.Text;

namespace PrevueCommander.Commands;

public class LocalAdCommand : BaseCommand
{
    private readonly int _index;
    private readonly byte[] _ad;
    
    public LocalAdCommand(int index, string ad) : base((byte)'L')
    {
        _index = index;
        _ad = Helpers.ConvertStringToBytes(ad, Helpers.AdFontTokenMapper);
    }

    protected override byte[] GetMessageBytes()
    {
        var messageBytes = new List<byte> { (byte) _index };
        messageBytes.AddRange(_ad);
        return messageBytes.ToArray();
    }
}