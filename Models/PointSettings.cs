using CommunityToolkit.Mvvm.ComponentModel;
using Mapper_v1.Core;
using Mapper_v1.Layers;
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
    private bool isObsoluteUnits = false;
}
