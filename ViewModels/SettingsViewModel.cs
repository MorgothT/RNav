using System.IO;
using System.Windows;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Mapper_v1.Contracts.Services;
using Mapper_v1.Contracts.ViewModels;
using Mapper_v1.Models;

using Microsoft.Extensions.Options;
using Microsoft.Win32;

namespace Mapper_v1.ViewModels;

// TODO: Change the URL for your privacy policy in the appsettings.json file, currently set to https://YourPrivacyUrlGoesHere
public partial class SettingsViewModel : ObservableObject, INavigationAware
{
    private readonly AppConfig _appConfig;
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ISystemService _systemService;
    private readonly IApplicationInfoService _applicationInfoService;
    private AppTheme _theme;
    private string _versionDescription;
    private ICommand _setThemeCommand;
    private ICommand _privacyStatementCommand;

    public AppTheme Theme
    {
        get { return _theme; }
        set { SetProperty(ref _theme, value); }
    }
    public string VersionDescription
    {
        get { return _versionDescription; }
        set { SetProperty(ref _versionDescription, value); }
    }
    public ICommand SetThemeCommand => _setThemeCommand ?? (_setThemeCommand = new RelayCommand<string>(OnSetTheme));
    public ICommand PrivacyStatementCommand => _privacyStatementCommand ?? (_privacyStatementCommand = new RelayCommand(OnPrivacyStatement));


    [ObservableProperty]
    private MapSettings mapSettings = new();
    [ObservableProperty]
    private CommSettings commSettings = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveDeviceCommand))]
    private DeviceSettings selectedDevice;


    [RelayCommand]
    private void AddDevice()
    {
        CommSettings.Devices.Add(new DeviceSettings());
        CommSettings.SaveSettings();
    }

    [RelayCommand(CanExecute = nameof(CanRemoveDevice))]
    private void RemoveDevice()
    {
        //int idx = CommSettings.Devices.IndexOf(SelectedDevice);
        CommSettings.Devices.Remove(SelectedDevice);
        CommSettings.SaveSettings();
    }
    private bool CanRemoveDevice()
    {
        return SelectedDevice is not null;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        MapSettings.SaveMapSettings();
        CommSettings.SaveSettings();
    }

    [RelayCommand]
    private void BrowseBoatShape()
    {
        OpenFileDialog ofd = new OpenFileDialog();
        ofd.Filter = @"Hypack Boat Shape (*.shp)|*.shp|CSV (*.csv)|*.csv";
        if (ofd.ShowDialog() == true)
        {
            MapSettings.BoatShape.Path = ofd.FileName;
            MapSettings.SaveMapSettings();
            MapSettings = new();
        }
    }
    
    public SettingsViewModel(IOptions<AppConfig> appConfig, IThemeSelectorService themeSelectorService, ISystemService systemService, IApplicationInfoService applicationInfoService)
    {
        _appConfig = appConfig.Value;
        _themeSelectorService = themeSelectorService;
        _systemService = systemService;
        _applicationInfoService = applicationInfoService;
    }

    public void OnNavigatedTo(object parameter)
    {
        VersionDescription = $"{Properties.Resources.AppDisplayName} - {_applicationInfoService.GetVersion().ToString(3)}";
        Theme = _themeSelectorService.GetCurrentTheme();
    }

    public void OnNavigatedFrom()
    {
    }

    private void OnSetTheme(string themeName)
    {
        var theme = (AppTheme)Enum.Parse(typeof(AppTheme), themeName);
        _themeSelectorService.SetTheme(theme);
    }

    private void OnPrivacyStatement()
        => _systemService.OpenInWebBrowser(_appConfig.PrivacyStatement);
}
