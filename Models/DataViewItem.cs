using CommunityToolkit.Mvvm.ComponentModel;

namespace Mapper_v1.Models;

public partial class DataViewItem : ObservableObject
{
	[ObservableProperty]
	private string name;

	[ObservableProperty]
	private string value;

	public DataViewItem(string name, string value)
	{
		Name = name;
		Value = value;
	}
}