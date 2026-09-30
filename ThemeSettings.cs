using Avalonia;
using Avalonia.Styling;

namespace IcoConverter;

public enum AppTheme
{
    System,
    Light,
    Dark
}

/// <summary>
/// A választott téma alkalmazása és megőrzése két indítás között
/// (%AppData%\IcoConverter\theme.txt).
/// </summary>
public static class ThemeSettings
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "IcoConverter", "theme.txt");

    public static AppTheme Current { get; private set; } = AppTheme.System;

    public static AppTheme Load()
    {
        try
        {
            if (File.Exists(FilePath) &&
                Enum.TryParse(File.ReadAllText(FilePath).Trim(), out AppTheme theme) &&
                Enum.IsDefined(theme))
                return theme;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Olvashatatlan beállítás: marad a rendszer témája.
        }

        return AppTheme.System;
    }

    public static void Apply(AppTheme theme)
    {
        Current = theme;
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = theme switch
            {
                AppTheme.Light => ThemeVariant.Light,
                AppTheme.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default // a rendszer beállítását követi
            };
        }
    }

    public static void Save(AppTheme theme)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, theme.ToString());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A téma ettől még érvényben marad, csak a következő indításkor nem emlékszünk rá.
        }
    }
}
