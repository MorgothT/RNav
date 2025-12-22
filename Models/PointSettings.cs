using CommunityToolkit.Mvvm.ComponentModel;
using netDxf.Header;
using System.Windows.Media;

namespace Mapper_v1.Models;

public partial class PointSettings : ObservableObject
{
    [ObservableProperty]
    private Color color = Colors.Red;
    [ObservableProperty]
    private double size = 10;
    [ObservableProperty]
    private PointShape shape = PointShape.Dot;
    [ObservableProperty]
    private bool isAbsoluteUnits = false;
}
