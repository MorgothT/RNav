using CommunityToolkit.Mvvm.ComponentModel;

namespace Mapper_v1.Models;

public partial class DataViewItem : ObservableObject
{
	[ObservableProperty]
	private string name;

	[ObservableProperty]
	private object value;

	public DataViewItem(string name, object value)
	{
		Name = name;
		Value = value;
	}
}