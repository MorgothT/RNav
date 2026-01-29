using System.Windows.Media;

namespace Mapper_v1.Models;

public class TargetSettings
{
    public float TargetRadius { get; set; } = 5.0f;
    public Color TargetColor { get; set; } = Colors.Green;
    public Color SelectedTargetColor { get; set; } = Colors.Red;
    public float TargetOpacity { get; set; } = 0.5f;
}
