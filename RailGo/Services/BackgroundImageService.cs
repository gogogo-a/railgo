using RailGo.Contracts.Services;
using RailGo.Helpers;

using WinRT.Interop;

using Windows.Storage;
using Windows.Storage.Pickers;

namespace RailGo.Services;

public class BackgroundImageService : IBackgroundImageService
{
    private const string SettingsKey = "AppCustomBackgroundImagePath";
    private const string NonMsixAppDataFolder = "RailGo/ApplicationData";
    private const string BackgroundFolderName = "Backgrounds";
    private const string BackgroundFilePrefix = "custom-background";

    private readonly ILocalSettingsService _localSettingsService;

    public string? BackgroundImagePath
    {
        get; private set;
    }

    public string? LastErrorMessage
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
        LastErrorMessage = null;

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

            var backgroundFolderPath = EnsureBackgroundFolder();
            DeleteExistingBackgroundFiles(backgroundFolderPath);

            var fileExtension = pickedFile.FileType;

            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                fileExtension = ".png";
            }

            var destinationPath = Path.Combine(backgroundFolderPath, $"{BackgroundFilePrefix}{fileExtension.ToLowerInvariant()}");
            File.Copy(pickedFile.Path, destinationPath, overwrite: true);

            BackgroundImagePath = destinationPath;
            await _localSettingsService.SaveSettingAsync(SettingsKey, BackgroundImagePath);
            BackgroundImageChanged?.Invoke(this, BackgroundImagePath);
            LastErrorMessage = null;

            return true;
        }
        catch (Exception ex)
        {
            LastErrorMessage = $"{ex.GetType().Name}: {ex.Message}";
            return false;
        }
    }

    public async Task ClearBackgroundImageAsync()
    {
        LastErrorMessage = null;

        var backgroundFolderPath = EnsureBackgroundFolder();
        DeleteExistingBackgroundFiles(backgroundFolderPath);

        BackgroundImagePath = null;
        await _localSettingsService.SaveSettingAsync<string?>(SettingsKey, null);
        BackgroundImageChanged?.Invoke(this, null);
    }

    private static void DeleteExistingBackgroundFiles(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return;
        }

        foreach (var filePath in Directory.EnumerateFiles(folderPath)
            .Where(path => Path.GetFileName(path).StartsWith(BackgroundFilePrefix, StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // Ignore files that cannot be deleted immediately.
            }
        }
    }

    private static string EnsureBackgroundFolder()
    {
        string baseFolder;

        if (RuntimeHelper.IsMSIX)
        {
            baseFolder = ApplicationData.Current.LocalFolder.Path;
        }
        else
        {
            baseFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                NonMsixAppDataFolder);
        }

        var backgroundFolderPath = Path.Combine(baseFolder, BackgroundFolderName);
        Directory.CreateDirectory(backgroundFolderPath);

        return backgroundFolderPath;
    }
}
