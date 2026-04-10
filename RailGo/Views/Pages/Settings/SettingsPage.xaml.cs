using RailGo.Contracts.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RailGo.ViewModels.Pages.Settings;
using RailGo.Views.Pages.Settings.DataSources;

namespace RailGo.Views.Pages.Settings;

public sealed partial class SettingsPage : Page
{
    private readonly IBackgroundImageService _backgroundImageService;

    public SettingsViewModel ViewModel
    {
        get;
    }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        _backgroundImageService = App.GetService<IBackgroundImageService>();
        InitializeComponent();
        this.Loaded += OnLoad;
        Unloaded += OnUnloaded;
    }

    public void OnLoad(object sender, RoutedEventArgs e)
    {
        InitializeThemeComboBox();
        UpdateBackgroundImageUiState();
        _backgroundImageService.BackgroundImageChanged -= OnBackgroundImageChanged;
        _backgroundImageService.BackgroundImageChanged += OnBackgroundImageChanged;
    }

    private void InitializeThemeComboBox()
    {
        if (ViewModel?.ElementTheme != null)
        {
            var theme = ViewModel.ElementTheme.ToString();

            foreach (ComboBoxItem item in ThemeComboBox.Items)
            {
                if (item.Tag?.ToString() == theme)
                {
                    ThemeComboBox.SelectedItem = item;
                    break;
                }
            }
        }
    }

    private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            var themeString = selectedItem.Tag?.ToString();

            if (Enum.TryParse<ElementTheme>(themeString, out var theme))
            {
                if (ViewModel?.ElementTheme != theme)
                {
                    ViewModel.SwitchThemeCommand.Execute(theme);
                }
            }
        }
    }
    private void OnDataSourcesSettingsCardClicked(object sender, RoutedEventArgs e)
    {
        DataSources_ShellPage page = new();

        TabViewItem tabViewItem = new()
        {
            Header = "数据源设置面板",
            Content = page,
            CanDrag = true,
            IconSource = new FontIconSource() { Glyph = "\uE7C0" }
        };
        MainWindow.Instance.MainTabView.TabItems.Add(tabViewItem);
        MainWindow.Instance.MainTabView.SelectedItem = tabViewItem;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _backgroundImageService.BackgroundImageChanged -= OnBackgroundImageChanged;
    }

    private async void OnChooseBackgroundImageClicked(object sender, RoutedEventArgs e)
    {
        var isSuccess = await _backgroundImageService.PickAndSetBackgroundImageAsync();
        if (!isSuccess)
        {
            BackgroundImagePathTextBlock.Text = "设置失败：请确认图片可访问，或重试选择 jpg/png/bmp/webp。";
            return;
        }

        UpdateBackgroundImageUiState();
    }

    private async void OnClearBackgroundImageClicked(object sender, RoutedEventArgs e)
    {
        await _backgroundImageService.ClearBackgroundImageAsync();
        UpdateBackgroundImageUiState();
    }

    private void OnBackgroundImageChanged(object? sender, string? imagePath)
    {
        if (DispatcherQueue.HasThreadAccess)
        {
            UpdateBackgroundImageUiState();
            return;
        }

        DispatcherQueue.TryEnqueue(UpdateBackgroundImageUiState);
    }

    private void UpdateBackgroundImageUiState()
    {
        var imagePath = _backgroundImageService.BackgroundImagePath;
        var hasCustomBackground = !string.IsNullOrWhiteSpace(imagePath);

        BackgroundImagePathTextBlock.Text = hasCustomBackground
            ? $"当前：{imagePath}"
            : "当前：默认背景";
        ClearBackgroundButton.IsEnabled = hasCustomBackground;
    }
}
