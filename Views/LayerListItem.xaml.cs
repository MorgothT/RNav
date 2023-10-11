using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Mapper_v1.Models;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Styles;

namespace Mapsui.Samples.Wpf;

public partial class LayerListItem
{
    public ILayer? Layer { get; set; }

    public string LayerName
    {
        set { TextBlock.Text = value; }
    }

    public double LayerOpacity
    {
        set { OpacitySlider.Value = value; }
    }

    public bool? Enabled
    {
        set { EnabledCheckBox.IsChecked = value; }
    }
    public System.Windows.Media.Color? Color
    {
        set { ColorSelector.SelectedColor = value; }
    }
    public LayerListItem()
    {
        InitializeComponent();
        OpacitySlider.IsMoveToPointEnabled = true; // mouse click moves slider to that specific position (otherwise only 0 or 1 is selected)
        //if ((string)Layer?.Tag == "GeoTiff")
        //    ColorSelector.Visibility = Visibility.Hidden;
    }

    private void OpacitySliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        var tempLayer = Layer;
        if (tempLayer != null)
        {
            tempLayer.Opacity = e.NewValue;
        }
    }

    private void EnabledCheckBoxClick(object sender, RoutedEventArgs e)
    {
        var tempLayer = Layer;
        if (tempLayer != null)
        {
            tempLayer.Enabled = ((CheckBox)e.Source).IsChecked != false;
        }
    }

    private void SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
    {
        var tempLayer = Layer;
        var selcol = ColorSelector.SelectedColor.Value;
        var pen = new Styles.Pen { Color = new Styles.Color(selcol.R, selcol.G, selcol.B, selcol.A)};
        if (tempLayer != null)
        {
            if (tempLayer.Style.GetType() == typeof(VectorStyle))
                tempLayer.Style = new VectorStyle { Line = pen };
            else
            {

            }
            
            
        }
        
    }
}
