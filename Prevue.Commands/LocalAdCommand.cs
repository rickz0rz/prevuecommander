using Prevue.Core;

namespace Prevue.Commands;

public class LocalAdCommand : BaseCommand
{
    private readonly byte[] _ad;
    private readonly int _index;

    public LocalAdCommand(int index, string ad) : base('L')
    {
        _index = index;
        _ad = Helpers.ConvertStringToBytes(ad, Helpers.AdFontTokenMapper);
    }

    public override string ToString()
    {
        return nameof(LocalAdCommand);
    }

    protected override byte[] GetMessageBytes()
    {
        var messageBytes = new List<byte> { (byte)_index };
        messageBytes.AddRange(_ad);
        return messageBytes.ToArray();
    }

    public static List<LocalAdCommand> GenerateAdCommands(string[] ads)
    {
        var commands = new List<LocalAdCommand>();
        for (var i = 0; i < ads.Length; i++) commands.Add(new LocalAdCommand(i + 1, ads[i]));
        return commands;
    }
}