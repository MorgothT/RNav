using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapper_v1.Contracts.Services;
using Mapper_v1.Contracts.ViewModels;
using Mapper_v1.Core.Contracts;
using Mapper_v1.Models;
using Mapper_v1.Projections;
using Mapper_v1.Services;
using Mapper_v1.Views;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Input;
using Velopack;
using Velopack.Sources;


namespace Mapper_v1.ViewModels;

// Change the URL for your privacy policy in the appsettings.json file, currently set to https://YourPrivacyUrlGoesHere
public partial class SettingsViewModel : ObservableObject, INavigationAware
{
    //private readonly ConfigService _configService;
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
    //private MapSettings mapSettings;
    private MapSettings mapSettings = new MapSettings().GetMapSettings();
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdateCommand))]
    private bool isUpdateRunning = false;

    [RelayCommand]
    private void SaveSettings()
    {
        
        MapSettings.SaveMapSettings();
        //CommSettings.SaveSettings();
    }

    [RelayCommand]
    private void BrowseLogDirectory()
    {
        var ofd = new OpenFolderDialog();
        ofd.InitialDirectory = MapSettings.LogDirectory;
        ofd.Multiselect = false;
        if (ofd.ShowDialog() == true)
        {
            MapSettings.LogDirectory = ofd.FolderName;
        }
    }
    [RelayCommand(CanExecute =nameof(CanUpdate))]
    private async Task Update()
    {
        try
        {
            IsUpdateRunning = true;
            await UpdateMyApp();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
        IsUpdateRunning = false;
    }
    private bool CanUpdate()
    {
        return IsUpdateRunning == false;
    }
    [RelayCommand]
    private void ExportSettings()
    {
        var sfd = new SaveFileDialog()
        {
            Filter = "Settings file (*.ini)|*.ini",
            //CheckFileExists = true,
            OverwritePrompt = true,
        };
        if (sfd.ShowDialog() == true)
        {
            MapSettings.SaveMapSettings(sfd.FileName);
            //_configService.SaveConfig(sfd.FileName);
        }
    }
    [RelayCommand]
    private void ImportSettings()
    {
        var ofd = new OpenFileDialog()
        {
            Multiselect = false,
            Filter = "Settings file (*.ini)|*.ini",
        };
        if (ofd.ShowDialog() == true)
        {
            MapSettings = new MapSettings().GetMapSettings(ofd.FileName);
            //_configService.LoadMapConfig(ofd.FileName);
        }
    }
    [RelayCommand]
    private void AddProjection()
    {
        //TODO: fix Projection adding window position - DONE
        // Create the dialog window and its ViewModel
        var dialogViewModel = new ShellDialogViewModel();
        var dialogWindow = new ShellDialogWindow(dialogViewModel)
        {
            Width = 380,
            Height = 200,
            BorderThickness = new Thickness(2),
            ShowMaxRestoreButton = false,
            ShowMinButton = false,
            // Set the owner to the main window to center it
            Owner = App.Current.MainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        // Create the DataDisplayDialogPage and its ViewModel
        var projectionViewModel = new ProjectionDialogViewModel();
        var projectionDialogPage = new ProjectionDialogPage(projectionViewModel);

        // Navigate the dialog's frame to the page
        dialogWindow.GetDialogFrame().Navigate(projectionDialogPage);
        // Show as dialog
        if (dialogWindow.ShowDialog() == true)
        {
            ProjectProjections.AddProjection(projectionViewModel.ProjectionWkt);
            MapSettings.ProjectionList = ProjectProjections.GetProjections();
            MapSettings.SaveMapSettings();
        }
    }
    public SettingsViewModel(IOptions<AppConfig> appConfig,
                             IThemeSelectorService themeSelectorService,
                             ISystemService systemService,
                             IApplicationInfoService applicationInfoService) //,ConfigService configService)
    {
        _appConfig = appConfig.Value;
        _themeSelectorService = themeSelectorService;
        _systemService = systemService;
        _applicationInfoService = applicationInfoService;
        //_configService = configService;
        //MapSettings = _configService.MapConfig;
    }
    private static async Task UpdateMyApp()
    {
        var mgr = new UpdateManager(new GithubSource("https://github.com/MorgothT/RNav", "", false));
        var newVersion = await mgr.CheckForUpdatesAsync();
        if (newVersion == null)
        {
            MessageBox.Show("This is the latest version available", "Updater", MessageBoxButton.OK, MessageBoxImage.Information);
            return;

        }
        await mgr.DownloadUpdatesAsync(newVersion);
        var result = MessageBox.Show($"Version {newVersion.TargetFullRelease.Version} is available.{Environment.NewLine}Do you wish to restart the application ?",
                                    "New version found",
                                    button: MessageBoxButton.YesNo,
                                    icon: MessageBoxImage.Question,
                                    defaultResult: MessageBoxResult.No);
        if (result == MessageBoxResult.Yes)
            mgr.ApplyUpdatesAndRestart(newVersion);
        else
            mgr.WaitExitThenApplyUpdates(newVersion.TargetFullRelease, restart: false);

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
