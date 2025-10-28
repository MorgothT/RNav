using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Media;
using ControlzEx.Theming;
using Mapper_v1.Helpers;

namespace Mapper_v1.Models;

public partial class DataViewItem : ObservableObject
{
    [ObservableProperty]
    private string name;
    [ObservableProperty]
    private string displayName;
    [ObservableProperty]
    private object value;
    [ObservableProperty]
    private bool highlight;
    [ObservableProperty]
    private Color? color;
    [ObservableProperty]
    private int decimalPlaces;
    [ObservableProperty]
    private Type type;
    [ObservableProperty]
    private Guid mobileId;
    [ObservableProperty]
    private int[] decimalPlacesOptions = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];
    [ObservableProperty]
    private bool showMobileName = true;
    [ObservableProperty]
    private double fontSize = 12;

    public static readonly DataViewItemComparer comparer = new();
    public static readonly Guid SystemId = new Guid("00000000-0000-0000-0000-000000000001");

    public DataViewItem(string name,
                        Type type,
                        Guid mobileId = default,
                        object value = null,
                        bool highlight = false,
                        Color? color = null,
                        int decimalPlaces = 2)
    {
        Name = name;
        Value = value;
        Type = type;
        Highlight = highlight;
        Color = color ?? Formater.GetThemeForegroundColor();
        DisplayName = GetDisplayName(name,ShowMobileName) ?? name;
        MobileId = mobileId;
        DecimalPlaces = decimalPlaces;
    }

    private static string GetDisplayName(string name, bool showMobileName = true)
    {
        int firstIndex = name.IndexOf(".");
        int lastIndex = name.LastIndexOf(".");
        if (showMobileName && firstIndex >= 0 && lastIndex >= 0)
        {
            return $"{name[..firstIndex]} {name[(lastIndex + 1)..]}";
        }
        //int index = name.LastIndexOf(".");
        if (lastIndex >= 0)
        {
            return name[(lastIndex + 1)..];
        }
        else
        {
            return name;
        }
    }
}

public class DataViewItemComparer : IEqualityComparer<DataViewItem>
{
    public bool Equals(DataViewItem x, DataViewItem y)
    {
        return x.Name == y.Name;
    }

    public int GetHashCode([DisallowNull] DataViewItem obj)
    {
        return base.GetHashCode();
    }
}
