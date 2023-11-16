using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoConverter;
using Mapper_v1.Models;
using Mapsui;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace Mapper_v1.ViewModels;

public partial class TargetsViewModel : ObservableObject
{
    #region Fields
    private Converter toWgs;
    #endregion

    #region Observable Properties

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveTargetCommand))]
    private Target selectedTarget;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportTargetsCommand), nameof(ClearTargetsCommand))]
    private MapSettings mapSettings = new();

    #endregion

    #region Constractor
    public TargetsViewModel() 
    {
        int epsg = int.Parse(mapSettings.CurrentProjection.Split(':')[1]);
        switch (epsg)
        {
            case 6991:
                toWgs = new( Converter.Projections.ITM,Converter.Ellipsoids.WGS_84);
                break;
            case 32636:
                toWgs = new(Converter.Projections.UTM_36N, Converter.Ellipsoids.WGS_84);
                break;
            default:
                toWgs = new(Converter.Ellipsoids.WGS_84, Converter.Ellipsoids.WGS_84);
                break;
        }
    }
    #endregion

    #region Commands

    [RelayCommand(CanExecute = nameof(CanExportTargets))]
    private void ExportTargets()
    {
        var sfd = new SaveFileDialog();
        sfd.AddExtension = true;
        sfd.Filter = "csv file (*.csv) | *.csv | target file (*.trg) | *.trg";
        if (sfd.ShowDialog() == true)
        {
            if (File.Exists(sfd.FileName)) { File.Delete(sfd.FileName); }
            foreach (Target target in MapSettings.TargetList)
            {
                //if (sfd.FileName.EndsWith(".csv"))    //if there is a need for another format
                //{
                    File.AppendAllText(sfd.FileName, $"{target}{Environment.NewLine}");
                //}
            }
        }
        
    }
    private bool CanExportTargets()
    {
        return MapSettings.TargetList.Count > 0;
    }

    [RelayCommand]
    private void ImportTargets()
    {
        var ofd = new OpenFileDialog();
        ofd.Filter = "target file (*.trg) | *.trg"; //other types might need parsing
        if (ofd.ShowDialog() == true)
        {
            try
            {
                int maxId = 0;
                if (MapSettings.TargetList is null) { MapSettings.TargetList = new(); }
                if (MapSettings.TargetList.Count > 0)
                {
                    maxId = MapSettings.TargetList.MaxBy(t => t.Id).Id;
                }
                string[] file = File.ReadAllLines(ofd.FileName);
                foreach (string line in file)
                {
                    Target target = Target.Parse(line);
                    if (target is not null)
                    {
                        maxId++;
                        target.Id = maxId;
                        MapSettings.TargetList.Add(target);
                    }
                }
                SaveTargets();
            }
            catch (Exception)
            {
            }
        }
        clearTargetsCommand.NotifyCanExecuteChanged();
        exportTargetsCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanExportTargets))]
    private void ClearTargets()
    {
        var result = MessageBox.Show($"This will delete all targets.{Environment.NewLine}Are you sure ?", "Clear Targets", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
        if (result == MessageBoxResult.Yes)
        {
            MapSettings.TargetList.Clear();
        }
        SaveTargets();
    }

    [RelayCommand]
    private void AddTarget()
    {
        AddNewTarget(new MPoint(0, 0));
        clearTargetsCommand.NotifyCanExecuteChanged();
        exportTargetsCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRemoveTarget))]
    private void RemoveTarget()
    {
        MapSettings.TargetList.Remove(SelectedTarget);
        MapSettings.SaveMapSettings();
        clearTargetsCommand.NotifyCanExecuteChanged();
        exportTargetsCommand.NotifyCanExecuteChanged();
    }
    private bool CanRemoveTarget()
    {
        return SelectedTarget != null;
    }

    [RelayCommand]
    private void Recalculate()
    {
        foreach (var target in MapSettings.TargetList)
        {
            RecalculateLatLon(target);
        }
    }

    [RelayCommand]
    private void SaveTargets()
    {
        MapSettings.SaveMapSettings();
    }

    #endregion

    #region Methods
    private Target AddNewTarget(MPoint point)
    {
        int id;
        if (MapSettings.TargetList is null)
        {
            MapSettings.TargetList = new();
        }
        id = MapSettings.TargetList.Count == 0 ? 0 : MapSettings.TargetList.Max(x => x.Id) + 1;
        Target target = Target.CreateTarget(point, id,toWgs);
        MapSettings.TargetList.Add(target);
        MapSettings.SaveMapSettings();
        return target;
    }

    private void RecalculateLatLon(Target target)
    {
        var ll = toWgs.Convert(new Converter.Point3d(target.X, target.Y, 0));
        var t = MapSettings.TargetList.Where(t => t.Equals(target)).FirstOrDefault();
        t.Lat = ll.Y;
        t.Lon = ll.X;
    }
    #endregion
}
