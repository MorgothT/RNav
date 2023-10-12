using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace Mapper_v1.Models;

public partial class DataViewItem : ObservableObject
{
	[ObservableProperty]
	private string name;
	[ObservableProperty]
	private object value;
	[ObservableProperty]
	private bool highlight;
	[ObservableProperty]
	private Color? color;

	public DataViewItem(string name, object value, bool highlight = false, Color? color = null )
	{
		Name = name;
		Value = value;
		Highlight = highlight;
		Color = color ?? Colors.Black;
	}
}