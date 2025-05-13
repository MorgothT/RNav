using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapper_v1.Core;
using Mapper_v1.Core.Contracts;
using Mapper_v1.Core.Models;
using Mapper_v1.Core.Models.Devices;
using Mapper_v1.Models;
using System.Collections.ObjectModel;

namespace Mapper_v1.ViewModels;

public partial class MobilesViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Mobile> mobiles;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveMobileCommand), nameof(AddDeviceCommand), nameof(RemoveDeviceCommand))]
    private Mobile selectedMobile;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveMobileCommand),nameof(AddDeviceCommand),nameof(RemoveDeviceCommand))]
    private object selectedDevice;

    //[ObservableProperty]
    //[NotifyCanExecuteChangedFor(nameof(RemoveMobileCommand), nameof(AddDeviceCommand), nameof(RemoveDeviceCommand))]
    //private DeviceType selectedDeviceType;

    private MobileSettings mobileSettings = new();

    public MobilesViewModel()
    {
        //Load From Settings
        LoadFromSettings();
        if (Mobiles is null)
        {
            Mobiles = [];
        }
        if (Mobiles.Count == 0)
        {
            Mobiles.Add(new Mobile());
        }
        
        //for debug & design
        
        //Mobiles.Add(DummyMobile());
        //SelectedDevice = Mobiles.FirstOrDefault().Devices.FirstOrDefault();
    }
    
    private Mobile DummyMobile()
    {
        Mobile dummy = new();
        dummy.DeviceConfigs.Add(new NmeaDeviceConfig());
        dummy.Name = "the One";
        return dummy;
    }

    #region Methods
    private void ImportFromFile()
    {

    }
    private void ExportToFile()
    {

    }
    private void LoadFromSettings()
    {
        Mobiles = mobileSettings.Mobiles;
    }
    private void SaveToSettings()
    {
        mobileSettings.SaveSettings(Mobiles);
    }
    #endregion

    #region Commands

    [RelayCommand]
    private void AddMobile()
    {
        Mobile mobile = new Mobile();
        Mobiles.Add(mobile);
        SelectedMobile = mobile;
    }

    [RelayCommand(CanExecute = nameof(CanRemoveMobile))]
    private void RemoveMobile()
    {
        int idx = Mobiles.IndexOf(SelectedMobile);
        Mobiles.Remove(SelectedMobile);
        if (Mobiles.Where(m => m.IsPrimery).Any() == false)
        {
            Mobiles.First().IsPrimery = true;
        }
        SelectedMobile = idx > 0 ? Mobiles[idx - 1] : Mobiles[0];
    }
    private bool CanRemoveMobile()
    {
        if (SelectedMobile is null) return false;
        if (Mobiles.Count <= 1) return false;
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanAddDevice))]
    private void AddDevice(object type)
    {
        switch (type)
        {
            //case DeviceType.None:
                //return;
            case DeviceType.NmeaDevice:
                SelectedDevice = new NmeaDeviceConfig();
                break;
            default:
                return;
        }
        //SelectedDevice = new NmeaDeviceConfig();    //TODO: diffrent devices imp...
        SelectedMobile.DeviceConfigs.Add((IDeviceConfig)SelectedDevice);
    }
    private bool CanAddDevice()
    {
        if (SelectedMobile is null) return false;
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanRemoveDevice))]
    private void RemoveDevice()
    {
        int idx = SelectedMobile.DeviceConfigs.IndexOf((IDeviceConfig)SelectedDevice);
        SelectedMobile.DeviceConfigs.Remove((IDeviceConfig)SelectedDevice);
        if (SelectedMobile.DeviceConfigs.Count > 0)
        {
            SelectedDevice = idx > 0 ? SelectedMobile.DeviceConfigs[idx - 1] : SelectedMobile.DeviceConfigs[0];
        }
        else SelectedDevice = null;
        //SelectedMobile.DeviceConfigs = [.. SelectedMobile.DeviceConfigs];
        //Mobiles = [.. Mobiles];
    }
    private bool CanRemoveDevice()
    {
        if (SelectedDevice is null || SelectedMobile is null) return false;
        return true;
    }

    [RelayCommand]
    private void SaveMobiles()
    {
        mobileSettings.SaveSettings(Mobiles);
    }

    #endregion
}

