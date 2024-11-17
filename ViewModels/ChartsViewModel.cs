using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapper_v1.Models;
using Microsoft.Win32;

namespace Mapper_v1.ViewModels;

public partial class ChartsViewModel : ObservableObject
{

    [ObservableProperty]
    private MapSettings mapSettings = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MoveDownCommand), nameof(MoveUpCommand),
        nameof(MoveTopCommand), nameof(MoveButtomCommand), nameof(RemoveChartCommand), nameof(ExportChartsCommand))]
    private ChartItem selectedChart;

    public ChartsViewModel()
    {
    }

    [RelayCommand]
    private void SaveCharts()
    {
        MapSettings.SaveMapSettings();
    }
    [RelayCommand]
    private void AddChart()
    {
        OpenFileDialog ofd = new OpenFileDialog
        {
            Filter = @"Vector files (*.shp;*.dxf)|*.shp;*.dxf|Raster files (*.tif;*.ecw)|*.tif;*.ecw|All files (*.*)|*.*"   // TODO: add more Chart types
        };
        if (ofd.ShowDialog() == true)
        {
            if (!MapSettings.ChartItems.Any(x => x.Path == ofd.FileName))
            {
                ChartItem chart = new(ofd.FileName);
                chart.Projection = MapSettings.CurrentProjection;
                MapSettings.ChartItems.Add(chart);
                SelectedChart = chart;
            }
        }
        //MapSettings.SaveMapSettings();
    }
    [RelayCommand(CanExecute = "CanMoveItem")]
    private void RemoveChart()
    {
        if (SelectedChart is not null)
        {
            int idx = MapSettings.ChartItems.IndexOf(SelectedChart);
            MapSettings.ChartItems.Remove(SelectedChart);
            if (MapSettings.ChartItems.Count > 0)
            {
                SelectedChart = idx < MapSettings.ChartItems.Count - 1 ? MapSettings.ChartItems[idx + 1] : MapSettings.ChartItems.Last();
            }
            //MapSettings.SaveMapSettings();
        }
    }
    [RelayCommand(CanExecute = "CanMoveItem")]
    private void MoveUp()
    {
        int idx = MapSettings.ChartItems.IndexOf(SelectedChart);
        if (idx > 0)
        {
            MapSettings.ChartItems.Move(idx, idx - 1);
            SelectedChart = MapSettings.ChartItems[idx - 1];
        }
        //MapSettings.SaveMapSettings();
    }
    [RelayCommand(CanExecute = "CanMoveItem")]
    private void MoveDown()
    {
        int idx = MapSettings.ChartItems.IndexOf(SelectedChart);
        if (idx < MapSettings.ChartItems.Count - 1)
        {
            MapSettings.ChartItems.Move(idx, idx + 1);
            SelectedChart = MapSettings.ChartItems[idx + 1];
        }
        //MapSettings.SaveMapSettings();
    }
    [RelayCommand(CanExecute = "CanMoveItem")]
    private void MoveTop()
    {
        int idx = MapSettings.ChartItems.IndexOf(SelectedChart);
        if (idx > 0)
        {
            MapSettings.ChartItems.Move(idx, 0);
            SelectedChart = MapSettings.ChartItems[0];
        }
        //MapSettings.SaveMapSettings();
    }
    [RelayCommand(CanExecute = "CanMoveItem")]
    private void MoveButtom()
    {
        int idx = MapSettings.ChartItems.IndexOf(SelectedChart);
        if (idx < MapSettings.ChartItems.Count - 1)
        {
            MapSettings.ChartItems.Move(idx, MapSettings.ChartItems.Count - 1);
            SelectedChart = MapSettings.ChartItems[MapSettings.ChartItems.Count - 1];
        }
        //MapSettings.SaveMapSettings();
    }
    private bool CanMoveItem()
    {
        return SelectedChart is not null;
    }

    [RelayCommand]
    private void ImportCharts()
    {
        MapSettings.LoadCharts();
    }
    [RelayCommand(CanExecute = nameof(CanExportCharts))]
    private void ExportCharts()
    {
        MapSettings.SaveCharts();
    }
    private bool CanExportCharts()
    {
        return MapSettings.ChartItems.Count > 0;
    }

}
