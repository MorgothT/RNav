using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapper_v1.Models;
using System.Collections.ObjectModel;

namespace Mapper_v1.ViewModels;

public partial class TargetsViewModel : ObservableObject
{

    #region Observable Properties
    
    [ObservableProperty]
    private Target selectedTarget;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportTargetsCommand), nameof(ClearTargetsCommand))]
    private MapSettings mapSettings = new();

    #endregion

    #region Constractor
    public TargetsViewModel() { }
    #endregion

    #region Commands

    [RelayCommand(CanExecute = nameof(CanExportTargets))]
    private void ExportTargets()
    {

    }
    private bool CanExportTargets()
    {
        return MapSettings.TargetList.Count > 0;
    }

    [RelayCommand]
    private void ImportTargets()
    {
        

        SaveTargets();
    }
    [RelayCommand(CanExecute = nameof(CanExportTargets))]
    private void ClearTargets()
    {
        //TODO: Are you sure (clear target)
        MapSettings.TargetList.Clear();
        SaveTargets();
    }

    [RelayCommand]
    private void SaveTargets()
    {
        MapSettings.SaveMapSettings();
    }

    #endregion
}
