using Avalonia.Media.Imaging;

namespace IcoConverter;

public sealed class ImageItem
{
    private const int ThumbnailWidth = 80;

    public ImageItem(string fullPath)
    {
        FullPath = fullPath;
        Thumbnail = LoadThumbnail(fullPath);
    }

    public string FullPath { get; }
    public string FileName => Path.GetFileName(FullPath);
    public string Folder => Path.GetDirectoryName(FullPath) ?? string.Empty;
    public Bitmap? Thumbnail { get; }

    private static Bitmap? LoadThumbnail(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return Bitmap.DecodeToWidth(stream, ThumbnailWidth);
        }
        catch (Exception)
        {
            // Sérült vagy nem olvasható kép: előnézet nélkül kerül a listába,
            // a hiba a konvertáláskor jelenik meg.
            return null;
        }
    }
}
