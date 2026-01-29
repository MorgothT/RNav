using Mapper_v1.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Mapper_v1.Views;

/// <summary>
/// Interaction logic for TargetsView.xaml
/// </summary>
public partial class TargetsPage : Page
{
    public TargetsPage(TargetsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
    {
        var header = sender as GridViewColumnHeader;
        if (header != null && header.Content.ToString() == "ID")
        {
            var view = CollectionViewSource.GetDefaultView(TargetListView.ItemsSource);
            if (view.SortDescriptions.Count > 0 && view.SortDescriptions[0].PropertyName == "Id")
            {
                // Toggle sort direction
                var current = view.SortDescriptions[0];
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription("Id", current.Direction == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending));
            }
            else
            {
                view.SortDescriptions.Clear();
                view.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
            }
        }
    }
}
