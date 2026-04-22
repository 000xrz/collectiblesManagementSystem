using System.Collections.ObjectModel;
using collectiblesManagementSystem.Services;
using Microsoft.Extensions.DependencyInjection;

namespace collectiblesManagementSystem;

public partial class App : Application
{
    public static ObservableCollection<Models.Collection> Collections { get; set; } = new();

    public App()
    {
        InitializeComponent();

        StorageService.LogPath();

        var loaded = StorageService.LoadAll();
        Collections = new ObservableCollection<Models.Collection>(loaded);
    }

    public static void SaveData()
    {
        StorageService.SaveAll(Collections.ToList());
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}