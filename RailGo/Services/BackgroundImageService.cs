using RailGo.Contracts.Services;

using WinRT.Interop;

using Windows.Storage;
using Windows.Storage.Pickers;

namespace RailGo.Services;

public class BackgroundImageService : IBackgroundImageService
{
    private const string SettingsKey = "AppCustomBackgroundImagePath";
    private const string BackgroundFolderName = "Backgrounds";
    private const string BackgroundFilePrefix = "custom-background";

    private readonly ILocalSettingsService _localSettingsService;

    public string? BackgroundImagePath
    {
        get; private set;
    }

    public event EventHandler<string?>? BackgroundImageChanged;

    public BackgroundImageService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        var cachedPath = await _localSettingsService.ReadSettingAsync<string>(SettingsKey);

        if (!string.IsNullOrWhiteSpace(cachedPath) && File.Exists(cachedPath))
        {
            BackgroundImagePath = cachedPath;
            return;
        }

        BackgroundImagePath = null;

        if (!string.IsNullOrWhiteSpace(cachedPath))
        {
            await _localSettingsService.SaveSettingAsync<string?>(SettingsKey, null);
        }
    }

    public async Task<bool> PickAndSetBackgroundImageAsync()
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".bmp");
            picker.FileTypeFilter.Add(".webp");

            var windowHandle = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(picker, windowHandle);

            var pickedFile = await picker.PickSingleFileAsync();

            if (pickedFile is null)
            {
                return false;
            }

            var backgroundFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(
                BackgroundFolderName,
                CreationCollisionOption.OpenIfExists);

            await DeleteExistingBackgroundFilesAsync(backgroundFolder);

            var fileExtension = pickedFile.FileType;

            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                fileExtension = ".png";
            }

            var copiedFile = await pickedFile.CopyAsync(
                backgroundFolder,
                $"{BackgroundFilePrefix}{fileExtension}",
                NameCollisionOption.ReplaceExisting);

            BackgroundImagePath = copiedFile.Path;
            await _localSettingsService.SaveSettingAsync(SettingsKey, BackgroundImagePath);
            BackgroundImageChanged?.Invoke(this, BackgroundImagePath);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task ClearBackgroundImageAsync()
    {
        var backgroundFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(
            BackgroundFolderName,
            CreationCollisionOption.OpenIfExists);

        await DeleteExistingBackgroundFilesAsync(backgroundFolder);

        BackgroundImagePath = null;
        await _localSettingsService.SaveSettingAsync<string?>(SettingsKey, null);
        BackgroundImageChanged?.Invoke(this, null);
    }

    private static async Task DeleteExistingBackgroundFilesAsync(StorageFolder folder)
    {
        var files = await folder.GetFilesAsync();

        foreach (var file in files.Where(file => file.Name.StartsWith(BackgroundFilePrefix, StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            catch
            {
                // Ignore files that cannot be deleted immediately.
            }
        }
    }
}
