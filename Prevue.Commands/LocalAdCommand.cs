using Prevue.Core;

namespace Prevue.Commands;

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
    
    public static List<LocalAdCommand> GenerateAdCommands(string[] ads)
    {
        var commands = new List<LocalAdCommand>();
        for (var i = 1; i <= ads.Length; i++)
        {
            commands.Add(new LocalAdCommand(i, ads[i - 1]));
        }
        return commands;
    }
}