using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Prevue.Commands;
using Prevue.Graphics;
using PrevueCommander.Model.PlaybookCommands;
using Xunit;

namespace PrevueCommander.Tests;

public class GraphicsTests
{
    private static readonly PlaybookContext Context =
        new(new DateTime(2026, 11, 12, 20, 0, 0), Path.GetTempPath(), TextWriter.Null);

    /// <summary>Makes an RGBA PNG file: the left half red, the right half transparent.</summary>
    private static byte[] Png(int width, int height)
    {
        var raw = new MemoryStream();
        for (var y = 0; y < height; y++)
        {
            raw.WriteByte(0);
            for (var x = 0; x < width; x++)
                raw.Write(x < width / 2 ? [255, 0, 0, 255] : [0, 0, 0, 0]);
        }

        var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
            raw.WriteTo(zlib);

        var file = new MemoryStream();
        file.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);
        var header = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(header, (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), (uint)height);
        header[8] = 8;
        header[9] = 6;
        Chunk(file, "IHDR", header);
        Chunk(file, "IDAT", compressed.ToArray());
        Chunk(file, "IEND", []);
        return file.ToArray();

        static void Chunk(Stream output, string type, byte[] data)
        {
            var length = new byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(length, (uint)data.Length);
            output.Write(length);
            output.Write(Encoding.ASCII.GetBytes(type));
            output.Write(data);
            output.Write(new byte[4]); // The reader does not check the CRC.
        }
    }

    [Fact]
    public void Png_IsRead()
    {
        var image = PngImage.Read(Png(4, 2));
        Assert.Equal((4, 2), (image.Width, image.Height));
        Assert.Equal(new byte[] { 255, 0, 0, 255 }, image.Pixels[..4]);
        Assert.Equal(0, image.Pixels[3 * 4 + 3]);
        Assert.Throws<InvalidDataException>(() => PngImage.Read([1, 2, 3]));
    }

    [Fact]
    public void Ad_HasTheFormatOfTheLogosOfTheDrive()
    {
        var picture = AdPicture.Create(PngImage.Read(Png(40, 20)), PictureLayout.LowResolution, PictureFit.Contain,
            AmigaColor.Parse("#000044"), keyBackground: true);
        Assert.Equal("FORM", Encoding.ASCII.GetString(picture, 0, 4));
        Assert.Equal("ILBMBMHD", Encoding.ASCII.GetString(picture, 8, 8));
        Assert.Equal(320, BinaryPrimitives.ReadUInt16BigEndian(picture.AsSpan(20)));
        Assert.Equal(240, BinaryPrimitives.ReadUInt16BigEndian(picture.AsSpan(22)));
        Assert.Equal(5, picture[28]);
    }

    [Fact]
    public void Ad_KeepsColorZeroForTheBackground()
    {
        var (palette, pixels) = AdPicture.Render(PngImage.Read(Png(40, 20)), PictureLayout.LowResolution,
            PictureFit.Stretch, AmigaColor.Parse("#000044"), keyBackground: true);
        // The left half is the red image, and the right half is transparent: the genlock key.
        Assert.Equal(0, pixels[300]);
        Assert.NotEqual(0, pixels[10]);
        Assert.Equal(new AmigaColor(15, 0, 0), palette[pixels[10]]);
    }

    [Fact]
    public void Banner_KeepsTheColorsOfTheGrid()
    {
        var (palette, _) = AdPicture.Render(PngImage.Read(Png(40, 20)), PictureLayout.Banner, PictureFit.Contain,
            AmigaColor.Parse("#000033"), keyBackground: false);
        Assert.Equal(PictureLayout.GridColors, palette.Take(4));
        Assert.Equal(new AmigaColor(0, 0, 3), palette[7]);
        Assert.Equal(8, palette.Count);
    }

    [Fact]
    public void Color_IsParsed()
    {
        Assert.Equal(new AmigaColor(0, 0, 4), AmigaColor.Parse("#000044"));
        Assert.Equal(new AmigaColor(15, 15, 0), AmigaColor.Parse("#ff0"));
        Assert.Throws<FormatException>(() => AmigaColor.Parse("navy"));
    }

    [Fact]
    public void ByteRun1_PacksRunsAndLiterals()
    {
        var output = new MemoryStream();
        IlbmImage.ByteRun1([7, 7, 7, 7, 1, 2, 3], output);
        Assert.Equal(new byte[] { 0xFD, 7, 2, 1, 2, 3 }, output.ToArray());
    }

    [Fact]
    public async Task GraphicAd_SendsThePictureToLogosAds()
    {
        var directory = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var source = Path.Combine(directory, "01-pizza.png");
            await File.WriteAllBytesAsync(source, Png(40, 20), TestContext.Current.CancellationToken);
            var command = new GraphicAdPlaybookCommand { Source = source, SaveTo = directory };
            var commands = await command.TransformAsync(Context, TestContext.Current.CancellationToken);

            Assert.Equal("Logos/Ads/01PIZZA,", command.LogoListLine);
            Assert.Equal(["32MakeDir RAM:Logos", "32MakeDir RAM:Logos/Ads", "32MakeDir DF0:Logos", "32MakeDir DF0:Logos/Ads"],
                commands.OfType<UtilityCommand>().Select(c => c.Text));
            Assert.Equal("DF0:Logos/Ads/01PIZZA", commands.OfType<FileTransferCommand>().Single().TargetPath);
            Assert.True(File.Exists(Path.Combine(directory, "01PIZZA")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Banner_SendsThePictureTheIniAndTheReload()
    {
        var source = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(source, Png(40, 20), TestContext.Current.CancellationToken);
            var commands = await new BannerPlaybookCommand { Source = source, Height = 80 }
                .TransformAsync(Context, TestContext.Current.CancellationToken);

            Assert.Equal(["DF0:Banner/ADBANNER.UV", "DF0:BANNER.INI"],
                commands.OfType<FileTransferCommand>().Select(c => c.TargetPath));
            Assert.Equal(UtilityAction.ReloadBanner, Assert.IsType<UtilityCommand>(commands[^1]).Action);
            await Assert.ThrowsAsync<ArgumentException>(() =>
                new BannerPlaybookCommand { Source = source, Height = 241 }.TransformAsync(Context, TestContext.Current.CancellationToken));
        }
        finally
        {
            File.Delete(source);
        }
    }

    [Fact]
    public async Task LogoList_SendsTheLinesWithCrLf()
    {
        var playbook = PlaybookLoader.Parse("""
            commands:
            - !!logoList
              lines: [ 'Logos/tvgsport.uv,', 'Logos/Ads/01PIZZA,' ]
            """);
        var commands = await playbook.Commands.Single().TransformAsync(Context, TestContext.Current.CancellationToken);
        var transfer = commands.OfType<FileTransferCommand>().Single();
        Assert.Equal("DF0:LOGO.LST", transfer.TargetPath);
        Assert.True(transfer.Render().AsSpan().IndexOf("Logos/tvgsport.uv,\r\nLogos/Ads/01PIZZA,\r\n"u8) >= 0);
        Assert.Empty(commands.OfType<UtilityCommand>());

        var reload = await new LogoListPlaybookCommand { Lines = ["Logos/tvgsport.uv,"], Reload = true }
            .TransformAsync(Context, TestContext.Current.CancellationToken);
        Assert.Equal(UtilityAction.ReloadLogoList, Assert.IsType<UtilityCommand>(reload[^1]).Action);
    }
}
