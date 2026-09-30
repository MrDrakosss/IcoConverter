using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;

namespace IcoConverter;

public partial class MainWindow : FAAppWindow
{
    private static readonly string[] SupportedExtensions = { ".png", ".jpg", ".jpeg" };
    private const string SameFolderHint = "A .ico a forráskép mappájába kerül";

    private readonly ObservableCollection<ImageItem> _items = new();
    private string? _outputFolder;
    private bool _busy;

    public MainWindow()
    {
        InitializeComponent();

        // Windows 11-en Mica háttér; ahol nem érhető el, marad a téma sima háttere.
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Mica, WindowTransparencyLevel.None };

        ThemeBox.SelectedIndex = (int)ThemeSettings.Current;

        FileList.ItemsSource = _items;
        _items.CollectionChanged += (_, _) =>
        {
            // A lista megváltozott, az előző konvertálás eredménye már nem erre vonatkozik.
            ResultBar.IsOpen = false;
            UpdateUiState();
        };

        SameFolderCheck.IsCheckedChanged += (_, _) => UpdateOutputFolderUi();
        UpdateOutputFolderUi();

        DropZone.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        DropZone.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        DropZone.AddHandler(DragDrop.DropEvent, OnDrop);

        UpdateUiState();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (ActualTransparencyLevel == WindowTransparencyLevel.Mica)
            Background = Brushes.Transparent;
    }

    private void UpdateUiState()
    {
        EmptyHint.IsVisible = _items.Count == 0;
        ImportButton.IsEnabled = !_busy;
        ClearButton.IsEnabled = !_busy && _items.Count > 0;
        ConvertButton.IsEnabled = !_busy && _items.Count > 0;
    }

    private void OnThemeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ThemeBox.SelectedIndex < 0)
            return;

        var theme = (AppTheme)ThemeBox.SelectedIndex;
        if (theme == ThemeSettings.Current)
            return;

        ThemeSettings.Apply(theme);
        ThemeSettings.Save(theme);
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        int added = 0;
        foreach (string path in paths)
        {
            if (!File.Exists(path)) continue;
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (!SupportedExtensions.Contains(ext)) continue;
            if (_items.Any(i => string.Equals(i.FullPath, path, StringComparison.OrdinalIgnoreCase)))
                continue;

            _items.Add(new ImageItem(path));
            added++;
        }

        if (added > 0)
            SetStatus($"{added} kép hozzáadva.");
        else
            SetStatus("Nincs támogatott kép (png, jpg, jpeg) a kiválasztásban.");
    }

    private async void OnImport(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Képek kiválasztása",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Képek") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg" } },
                FilePickerFileTypes.All
            }
        });

        if (files.Count > 0)
            AddFiles(LocalPaths(files));
    }

    private void OnClear(object? sender, RoutedEventArgs e)
    {
        _items.Clear();
        ResultBar.IsOpen = false;
        SetStatus(string.Empty);
    }

    private void OnRemoveItem(object? sender, RoutedEventArgs e)
    {
        if (!_busy && sender is Button { DataContext: ImageItem item })
            _items.Remove(item);
    }

    private void OnFileListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete || _busy || FileList.SelectedItems is null)
            return;

        foreach (ImageItem item in FileList.SelectedItems.OfType<ImageItem>().ToList())
            _items.Remove(item);
        e.Handled = true;
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        bool accept = !_busy && e.DataTransfer.Contains(DataFormat.File);
        e.DragEffects = accept ? DragDropEffects.Copy : DragDropEffects.None;
        DropZone.Classes.Set("dragover", accept);
        e.Handled = true;
    }

    private void OnDragLeave(object? sender, DragEventArgs e) =>
        DropZone.Classes.Set("dragover", false);

    private void OnDrop(object? sender, DragEventArgs e)
    {
        DropZone.Classes.Set("dragover", false);
        if (_busy)
            return;

        if (e.DataTransfer.TryGetFiles() is { } files)
            AddFiles(LocalPaths(files));
    }

    private static IEnumerable<string> LocalPaths(IEnumerable<IStorageItem> items) =>
        items.Select(i => i.TryGetLocalPath()).OfType<string>();

    private void UpdateOutputFolderUi()
    {
        bool sameFolder = SameFolderCheck.IsChecked == true;
        OutputFolderBox.IsEnabled = !sameFolder;
        BrowseButton.IsEnabled = !sameFolder;
        OutputFolderBox.Text = sameFolder ? SameFolderHint : _outputFolder ?? string.Empty;
    }

    private async void OnBrowseFolder(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Kimeneti mappa"
        });

        if (folders.Count > 0 && folders[0].TryGetLocalPath() is { } path)
        {
            _outputFolder = path;
            UpdateOutputFolderUi();
        }
    }

    private async void OnConvert(object? sender, RoutedEventArgs e)
    {
        bool sameFolder = SameFolderCheck.IsChecked == true;
        string? outputFolder = sameFolder ? null : _outputFolder;

        if (!sameFolder && (string.IsNullOrWhiteSpace(outputFolder) || !Directory.Exists(outputFolder)))
        {
            ShowResult(FAInfoBarSeverity.Warning, "Hiányzó mappa",
                "Válassz érvényes kimeneti mappát, vagy pipáld be a „Mentés a képek mellé” lehetőséget.");
            return;
        }

        List<ImageItem> items = _items.ToList();
        var progress = new Progress<int>(done => SetStatus($"Konvertálás… {done}/{items.Count}"));

        _busy = true;
        UpdateUiState();
        ResultBar.IsOpen = false;
        SetStatus($"Konvertálás… 0/{items.Count}");

        int ok = 0;
        var errors = new List<string>();

        try
        {
            await Task.Run(() =>
            {
                for (int i = 0; i < items.Count; i++)
                {
                    ImageItem item = items[i];
                    try
                    {
                        string targetFolder = outputFolder ?? Path.GetDirectoryName(item.FullPath)!;
                        string destination = Path.Combine(
                            targetFolder,
                            Path.GetFileNameWithoutExtension(item.FullPath) + ".ico");

                        IcoWriter.Convert(item.FullPath, destination);
                        ok++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{item.FileName}: {ex.Message}");
                    }

                    ((IProgress<int>)progress).Report(i + 1);
                }
            });
        }
        finally
        {
            _busy = false;
            UpdateUiState();
        }

        if (errors.Count == 0)
        {
            SetStatus($"Kész: {ok} .ico fájl elmentve.");
            ShowResult(FAInfoBarSeverity.Success, "Kész",
                $"{ok} kép sikeresen átalakítva .ico formátumba.");
        }
        else
        {
            SetStatus($"Kész: {ok} sikeres, {errors.Count} hiba.");
            ShowResult(ok > 0 ? FAInfoBarSeverity.Warning : FAInfoBarSeverity.Error,
                ok > 0 ? "Részben sikerült" : "Nem sikerült",
                $"{ok} sikeres átalakítás, {errors.Count} hiba.");

            await new FAContentDialog
            {
                Title = ok > 0 ? "Részben sikerült" : "Nem sikerült",
                Content = new ScrollViewer
                {
                    MaxHeight = 280,
                    Content = new TextBlock
                    {
                        Text = $"{ok} sikeres átalakítás.\n\nHibák:\n{string.Join("\n", errors)}",
                        TextWrapping = TextWrapping.Wrap
                    }
                },
                CloseButtonText = "Bezárás"
            }.ShowAsync(this);
        }
    }

    private void ShowResult(FAInfoBarSeverity severity, string title, string message)
    {
        ResultBar.Severity = severity;
        ResultBar.Title = title;
        ResultBar.Message = message;
        ResultBar.IsOpen = true;
    }

    private void SetStatus(string text) => StatusText.Text = text;
}
