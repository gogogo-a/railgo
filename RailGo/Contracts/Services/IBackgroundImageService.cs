namespace RailGo.Contracts.Services;

public interface IBackgroundImageService
{
    string? BackgroundImagePath
    {
        get;
    }

    string? LastErrorMessage
    {
        get;
    }

    event EventHandler<string?>? BackgroundImageChanged;

    Task InitializeAsync();

    Task<bool> PickAndSetBackgroundImageAsync();

    Task ClearBackgroundImageAsync();
}
