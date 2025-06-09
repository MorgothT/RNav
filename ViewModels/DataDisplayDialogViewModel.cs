using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapper_v1.Contracts.ViewModels;
using Mapper_v1.Models;
using System.Collections.ObjectModel;

namespace Mapper_v1.ViewModels;

public partial class DataDisplayDialogViewModel : ObservableObject, INavigationAware
{
    [ObservableProperty]
    private ObservableCollection<DataViewItem> availableItems = new();

    [ObservableProperty]
    private ObservableCollection<DataViewItem> selectedItems = new();

    [ObservableProperty]
    private DataViewItem selectedAvailableItem;

    [ObservableProperty]
    private DataViewItem selectedSelectedItem;

    public DataDisplayDialogViewModel(ObservableCollection<DataViewItem> dataViewItems, MobileSettings mobileSettings)
    {
        // Example data for demonstration
        // Populate the available items from mobile settings
        var comparer = new DataViewItemComparer();
        SelectedItems = dataViewItems ?? new ObservableCollection<DataViewItem>();
        RemoveInvalidSelectedItems(mobileSettings);
        AvailableItems = new ObservableCollection<DataViewItem>(
            PopulateAvailableItems(mobileSettings).Except(SelectedItems, comparer)
        );
    }

    private void RemoveInvalidSelectedItems(MobileSettings mobileSettings)
    {
        var temp = new ObservableCollection<DataViewItem>(SelectedItems);
        foreach (var item in temp)
        {
            if (item.MobileId == DataViewItem.SystemId) continue;
            if (mobileSettings.Mobiles.Where(c=>c.Id == item.MobileId).Count() == 0)
            {
                SelectedItems.Remove(item);
            }
        }
    }

    private static ObservableCollection<DataViewItem> PopulateAvailableItems(MobileSettings mobileSettings)
    {
        // Populate the available items from mobile settings
        ObservableCollection<DataViewItem> itemList = new()
        {
            new DataViewItem("LOGGING", typeof(bool), DataViewItem.SystemId,false)
        };
        
        int mobileIndex = 1;
        foreach (var mobile in mobileSettings.Mobiles)
        {
            string mobilePrefix = $"{mobile.Name ?? $"Mobile{mobileIndex}"}";
            AddPropertiesRecursive(mobile.Data, mobilePrefix, itemList,mobile.Id);
            mobileIndex++;
        }
        AddDefaultItems(itemList);
        return itemList;
    }

    private static void AddDefaultItems(ObservableCollection<DataViewItem> itemList)
    {
        itemList.Add(new DataViewItem("Target X", typeof(double), DataViewItem.SystemId));
        itemList.Add(new DataViewItem("Target Y", typeof(double), DataViewItem.SystemId));
        itemList.Add(new DataViewItem("Target Dist", typeof(double), DataViewItem.SystemId));
        itemList.Add(new DataViewItem("Target Brg", typeof(double), DataViewItem.SystemId));
        itemList.Add(new DataViewItem("Target Name", typeof(string), DataViewItem.SystemId));
    }

    private static void AddPropertiesRecursive(object obj, string prefix, ObservableCollection<DataViewItem> itemList, Guid id)
    {
        if (obj == null) return;
        var type = obj.GetType();
        foreach (var prop in type.GetProperties())
        {
            // Ignore function (delegate) properties
            if (typeof(Delegate).IsAssignableFrom(prop.PropertyType))
                continue;

            var value = prop.GetValue(obj);
            string propName = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
            Type propType = prop.PropertyType;

            // Recurse for complex types (excluding string and value types)
            if (value != null
                && !(value is string)
                && !prop.PropertyType.IsValueType
                && !prop.PropertyType.IsEnum)
            {
                AddPropertiesRecursive(value, propName, itemList, id);
            }
            else
            {
                itemList.Add(new DataViewItem(propName, propType, id));
            }
        }
    }

    [RelayCommand]
    private void MoveToSelected()
    {
        var item = SelectedAvailableItem;
        // Check if the item is already in SelectedItems to avoid duplicates
        if (item != null && AvailableItems.Contains(item))
        {
            int index = AvailableItems.IndexOf(item);
            AvailableItems.Remove(item);
            SelectedItems.Add(item);
            SelectedSelectedItem = item;
            SelectedAvailableItem = AvailableItems.ElementAtOrDefault(index);
        }
    }

    [RelayCommand]
    private void MoveToAvailable()
    {
        var item = SelectedSelectedItem;
        // Check if the item is already in AvailableItems to avoid duplicates
        if (item != null && SelectedItems.Contains(item))
        {
            int index = SelectedItems.IndexOf(item);
            SelectedItems.Remove(item);
            AvailableItems.Add(item);
            SelectedAvailableItem = item;
            SelectedSelectedItem = SelectedItems.ElementAtOrDefault(index);
        }
    }
    public void OnNavigatedFrom()
    {
        // Not implemented
    }

    public void OnNavigatedTo(object parameter)
    {
        // Not implemented
    }
}
