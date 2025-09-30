using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeoConverter;
using Mapper_v1.Core.Models;
using Mapper_v1.Helpers;
using Mapper_v1.Models;
using Mapsui;
using Mapsui.Projections;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.IO;
using System.Windows;

namespace Mapper_v1.ViewModels;

public partial class TargetsViewModel : ObservableObject
{
    #region Fields
    //private Converter toWgs;
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
    }
    #endregion

    #region Commands

    [RelayCommand(CanExecute = nameof(CanExportTargets))]
    private void ExportTargets()
    {
        var sfd = new SaveFileDialog
        {
            AddExtension = true,
            Filter = "csv file (*.csv) | *.csv | target file (*.trg) | *.trg",
            FilterIndex = 2,
        };
        if (sfd.ShowDialog() == true)
        {
            if (File.Exists(sfd.FileName)) { File.Delete(sfd.FileName); }
            string targets = JsonConvert.SerializeObject(MapSettings.TargetList);
            File.WriteAllText(sfd.FileName, targets);
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
                string targets = File.ReadAllText(ofd.FileName);
                List<Target> importedTargets = JsonConvert.DeserializeObject<List<Target>>(targets);
                foreach (Target target in importedTargets)
                {
                    if (MapSettings.TargetList.Where(x=> x.Equals(target)).Any())    //Skip if same target allready exists
                    {
                        continue; 
                    }
                    if (MapSettings.TargetList.Where(x => x.Id == target.Id).Any() == true) //found target with same Id
                    {
                        maxId++;
                        target.Id = maxId;
                    }
                    MapSettings.TargetList.Add(target);
                }
                //string[] file = File.ReadAllLines(ofd.FileName);
                //foreach (string line in file)
                //{
                //    Target target = Target.Parse(line);
                //    if (target is not null)
                //    {
                //        maxId++;
                //        target.Id = maxId;
                //        MapSettings.TargetList.Add(target);
                //    }
                //}
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
        //MPoint wgsPoint = new(point);
        //wgsPoint.ToWgs($"EPSG:{MapSettings.CurrentProjection.Split(':')[1]}");
        Target target = Target.CreateTarget(point, id, point.ToWgs($"EPSG:{MapSettings.CurrentProjection.Split(':')[1]}"));
        MapSettings.TargetList.Add(target);
        MapSettings.SaveMapSettings();
        return target;
    }

    private void RecalculateLatLon(Target target)
    {
        MPoint wgs = target.ToMPoint().ToWgs($"EPSG:{MapSettings.CurrentProjection.Split(':')[1]}");
        //ProjectionDefaults.Projection.Project($"EPSG:{MapSettings.CurrentProjection.Split(':')[1]}", "EPSG:4326",wgs);
        var t = MapSettings.TargetList.Where(t => t.Equals(target)).FirstOrDefault();
        //var ll = toWgs.Convert(new Converter.Point3d(target.X, target.Y, 0));
        //t.Lat = ll.Y;
        //t.Lon = ll.X;
        t.Lon = wgs.X; 
        t.Lat = wgs.Y;
    }
    #endregion
}
