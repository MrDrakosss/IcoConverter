using Avalonia;
using Avalonia.Media.Imaging;

namespace IcoConverter;

/// <summary>
/// PNG/JPG/JPEG képet .ico fájllá alakít több felbontású, PNG-tömörített
/// bejegyzésekkel. A PNG-alapú ICO formátumot a Windows Vista és újabb
/// (így Windows 10 / 11) natívan támogatja.
/// </summary>
public static class IcoWriter
{
    // A klasszikus ikon-méretek, amelyeket a Windows használ.
    private static readonly int[] Sizes = { 16, 24, 32, 48, 64, 128, 256 };

    /// <summary>
    /// Betölti a forrásképet és a megadott útvonalra ír egy .ico fájlt.
    /// </summary>
    public static void Convert(string sourcePath, string destinationPath)
    {
        using Bitmap source = LoadImage(sourcePath);

        // A forrás natív mérete (négyzetesnél a szélesség = magasság).
        int sourceMax = Math.Max(source.PixelSize.Width, source.PixelSize.Height);

        // Csak olyan méreteket generálunk, amelyek nem nagyobbak a forrásnál
        // (nincs elmosódott felnagyítás), de a forrás natív méretét mindig belevesszük.
        var targetSizes = new SortedSet<int>(Sizes.Where(s => s <= sourceMax));
        targetSizes.Add(Math.Min(sourceMax, 256));

        // FONTOS: a bejegyzéseket a legnagyobbtól a legkisebbig írjuk ki, így a
        // Windows és a legtöbb ikonnéző a nagy felbontású képet mutatja alapból.
        var pngFrames = new List<(int size, byte[] data)>();
        foreach (int size in targetSizes.Reverse())
        {
            pngFrames.Add((size, EncodePng(source, size)));
        }

        WriteIco(destinationPath, pngFrames);
    }

    private static Bitmap LoadImage(string path)
    {
        // A stream a betöltés után lezárul: a fájl nem marad zárolva.
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
        return new Bitmap(stream);
    }

    private static byte[] EncodePng(Bitmap source, int size)
    {
        using Bitmap scaled = source.CreateScaledBitmap(
            new PixelSize(size, size), BitmapInterpolationMode.HighQuality);
        using var ms = new MemoryStream();
        scaled.Save(ms, PngBitmapEncoderOptions.Default);
        return ms.ToArray();
    }

    private static void WriteIco(string destinationPath, List<(int size, byte[] data)> frames)
    {
        using var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(fs);

        // ICONDIR fejléc
        writer.Write((ushort)0);              // reserved
        writer.Write((ushort)1);              // type: 1 = ikon
        writer.Write((ushort)frames.Count);   // képek száma

        // Az első kép adata a fejléc + az összes bejegyzés után kezdődik.
        int offset = 6 + frames.Count * 16;

        foreach (var (size, data) in frames)
        {
            // ICONDIRENTRY (16 bájt)
            writer.Write((byte)(size >= 256 ? 0 : size)); // szélesség (0 = 256)
            writer.Write((byte)(size >= 256 ? 0 : size)); // magasság (0 = 256)
            writer.Write((byte)0);   // színek száma a palettában (0 = nincs)
            writer.Write((byte)0);   // reserved
            writer.Write((ushort)1); // color planes
            writer.Write((ushort)32);// bit/pixel
            writer.Write((uint)data.Length); // a képadat mérete
            writer.Write((uint)offset);      // a képadat eltolása
            offset += data.Length;
        }

        // A tényleges PNG-adatok
        foreach (var (_, data) in frames)
        {
            writer.Write(data);
        }
    }
}
