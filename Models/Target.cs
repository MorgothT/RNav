using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui;
using System.Windows;

namespace Mapper_v1.Models;

public partial class Target : ObservableObject
{

    [ObservableProperty]
    private int id;
    [ObservableProperty]
	private string name = "";
	[ObservableProperty]
    private double x;
    [ObservableProperty]
    private double y;
    [ObservableProperty]
    private double lat;
    [ObservableProperty]
    private double lon;

    /// <summary>
    /// Target point (unaware of EPSG !)
    /// </summary>
    public Target()
    {
    }

    public override string ToString()
    {
        return $"{Id},{Name},{X},{Y},{Lat},{Lon}";    
    }
}
