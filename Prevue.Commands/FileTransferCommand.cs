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
/// <para>
/// ESQ cannot open the temporary file if its directory does not exist in RAM:, and then it ignores the transfer. The
/// copy to the target also fails if the target directory does not exist. For a target in a directory, make these
/// directories first (see <see cref="Directories"/>).
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

    /// <summary>
    /// The directories that the transfer needs, from the top directory down: first in RAM: for the temporary file,
    /// then on the target volume. For example, "DF0:Logos/Ads/AD.IFF" needs "RAM:Logos", "RAM:Logos/Ads", "DF0:Logos"
    /// and "DF0:Logos/Ads". A file at the top of a volume needs none.
    /// </summary>
    public IEnumerable<string> Directories()
    {
        var parts = TargetPath[4..].Split('/');
        foreach (var volume in new[] { "RAM:", TargetPath[..4] })
        {
            for (var i = 1; i < parts.Length; i++)
                yield return volume + string.Join('/', parts[..i]);
        }
    }

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
