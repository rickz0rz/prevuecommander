namespace Prevue.Commands;

/// <summary>
/// The 'H' command: sends a file to the drive of ESQ, for example a logo or a graphic ad. ESQ accepts it only after
/// a matching 'A' command (<c>_DISKIO2_HandleInteractiveFileTransfer</c>).
/// </summary>
/// <remarks>
/// <para>
/// The transfer has a header, blocks and an end. The header is a normal command with the target path as its
/// payload. Each block is <c>55 AA 'H'</c>, a sequence number that starts at 0, the length of the data (1 to 255),
/// the data and the XOR checksum of all the bytes before it. The end is a block with the length 0 and no data.
/// </para>
/// <para>
/// ESQ writes the data to RAM: first. For the temporary file, it replaces the first 4 characters of the target path
/// with "RAM:", so the path must start with a volume name of 4 characters, for example "DF0:". At the end, ESQ copies
/// the file to the target path.
/// </para>
/// </remarks>
public class FileTransferCommand : BaseCommand
{
    /// <summary>ESQ reads a maximum of 31 characters of the path.</summary>
    public const int MaximumPathLength = 31;

    private const int BlockSize = 255;

    private readonly byte[] _content;

    public FileTransferCommand(string targetPath, byte[] content) : base('H')
    {
        if (targetPath.Length is < 5 or > MaximumPathLength || targetPath[3] != ':' ||
            !targetPath.All(char.IsAscii))
            throw new ArgumentException(
                $"The target path \"{targetPath}\" must start with a volume such as \"DF0:\" and have a maximum of " +
                $"{MaximumPathLength} ASCII characters.", nameof(targetPath));

        TargetPath = targetPath;
        _content = content;
    }

    public string TargetPath { get; }

    public override string ToString() => $"{nameof(FileTransferCommand)}: {TargetPath} ({_content.Length} bytes)";

    protected override byte[] GetPayload() => Ascii(TargetPath);

    public override byte[] Render()
    {
        var bytes = new List<byte>(base.Render());
        byte sequence = 0;
        foreach (var block in _content.Chunk(BlockSize))
            bytes.AddRange(Frame([(byte)'H', sequence++, (byte)block.Length, .. block]));
        bytes.AddRange(Frame([(byte)'H', sequence, 0x00]));
        return bytes.ToArray();
    }
}
