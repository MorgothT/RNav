using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvernessPark.Utilities.NMEA.Sentences;
using Mapper_v1.Models;
using Mapper_v1.Views;
using Mapsui.Tiling;
using System.Drawing;

namespace Mapper_v1.ViewModels;

public partial class MapViewModel : ObservableObject
{

    [ObservableProperty]
    private List<DataViewItem> dataView = new();

    public MapViewModel()
    {

    }


    [RelayCommand]
    private void ChangeData()
    {
        List<DataViewItem> list = new()
        {
            new DataViewItem("X", "0"),
            new DataViewItem("Y", "0")
        };
        DataView = list;
    }
}
