﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MahApps.Metro.Controls;
using Mapper_v1.Contracts.Services;
using Mapper_v1.Properties;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Mapper_v1.ViewModels;
public class ShellViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private HamburgerMenuItem _selectedMenuItem;
    private HamburgerMenuItem _selectedOptionsMenuItem;
    private RelayCommand _goBackCommand;
    private ICommand _menuItemInvokedCommand;
    private ICommand _optionsMenuItemInvokedCommand;
    private ICommand _loadedCommand;
    private ICommand _unloadedCommand;

    public HamburgerMenuItem SelectedMenuItem
    {
        get { return _selectedMenuItem; }
        set { SetProperty(ref _selectedMenuItem, value); }
    }
    public HamburgerMenuItem SelectedOptionsMenuItem
    {
        get { return _selectedOptionsMenuItem; }
        set { SetProperty(ref _selectedOptionsMenuItem, value); }
    }

    // Change the icons and titles for all HamburgerMenuItems here.
    public ObservableCollection<HamburgerMenuItem> MenuItems { get; } = new ObservableCollection<HamburgerMenuItem>()
    {
        new HamburgerMenuGlyphItem() { Label = Resources.ShellMapPage, Glyph = "\uE722", TargetPageType = typeof(MapViewModel),ToolTip = "Map" },
        new HamburgerMenuGlyphItem() { Label = Resources.ShellChartsPage, Glyph = "\ue71c", TargetPageType = typeof(ChartsViewModel), ToolTip = "Charts" },
        new HamburgerMenuGlyphItem() { Label = Resources.ShellTargetsPage, Glyph = "\ue701", TargetPageType = typeof(TargetsViewModel), ToolTip = "Targets" },
    };
    public ObservableCollection<HamburgerMenuItem> OptionMenuItems { get; } = new ObservableCollection<HamburgerMenuItem>()
    {
        new HamburgerMenuGlyphItem() { Label = Resources.ShellSettingsPage, Glyph = "\uE720", TargetPageType = typeof(SettingsViewModel), ToolTip = "Settings" }
    };
    public RelayCommand GoBackCommand => _goBackCommand ?? (_goBackCommand = new RelayCommand(OnGoBack, CanGoBack));
    public ICommand MenuItemInvokedCommand => _menuItemInvokedCommand ?? (_menuItemInvokedCommand = new RelayCommand(OnMenuItemInvoked));
    public ICommand OptionsMenuItemInvokedCommand => _optionsMenuItemInvokedCommand ?? (_optionsMenuItemInvokedCommand = new RelayCommand(OnOptionsMenuItemInvoked));
    public ICommand LoadedCommand => _loadedCommand ?? (_loadedCommand = new RelayCommand(OnLoaded));
    public ICommand UnloadedCommand => _unloadedCommand ?? (_unloadedCommand = new RelayCommand(OnUnloaded));
    public ShellViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }
    private void OnLoaded()
    {
        _navigationService.Navigated += OnNavigated;
    }
    private void OnUnloaded()
    {
        _navigationService.Navigated -= OnNavigated;
    }
    private bool CanGoBack()
        => _navigationService.CanGoBack;
    private void OnGoBack()
        => _navigationService.GoBack();
    private void OnMenuItemInvoked()
        => NavigateTo(SelectedMenuItem.TargetPageType);
    private void OnOptionsMenuItemInvoked()
        => NavigateTo(SelectedOptionsMenuItem.TargetPageType);
    private void NavigateTo(Type targetViewModel)
    {
        if (targetViewModel != null)
        {
            _navigationService.NavigateTo(targetViewModel.FullName);
        }
    }
    private void OnNavigated(object sender, string viewModelName)
    {
        var item = MenuItems
                    .OfType<HamburgerMenuItem>()
                    .FirstOrDefault(i => viewModelName == i.TargetPageType?.FullName);
        if (item != null)
        {
            SelectedMenuItem = item;
        }
        else
        {
            SelectedOptionsMenuItem = OptionMenuItems
                    .OfType<HamburgerMenuItem>()
                    .FirstOrDefault(i => viewModelName == i.TargetPageType?.FullName);
        }
        GoBackCommand.NotifyCanExecuteChanged();
    }

}
