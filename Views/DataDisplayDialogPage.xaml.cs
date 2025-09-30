using System.Windows.Controls;
using System.Windows.Input;
using Mapper_v1.ViewModels;
using Mapper_v1.Models;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Mapper_v1.Views;

public partial class DataDisplayDialogPage : Page
{
    private Point _dragStartPoint;

    public DataDisplayDialogPage(DataDisplayDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void AvailableListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is DataViewItem item)
        {
            if (DataContext is DataDisplayDialogViewModel vm && vm.MoveToSelectedCommand.CanExecute(item))
            {
                vm.MoveToSelectedCommand.Execute(item);
            }
        }
    }

    private void SelectedListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListView listView && listView.SelectedItem is DataViewItem item)
        {
            if (DataContext is DataDisplayDialogViewModel vm && vm.MoveToAvailableCommand.CanExecute(item))
            {
                vm.MoveToAvailableCommand.Execute(item);
            }
        }
    }

    private void SelectedListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void SelectedListView_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            Point position = e.GetPosition(null);
            if (Math.Abs(position.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(position.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                if (sender is ListView listView)
                {
                    var listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
                    if (listViewItem != null)
                    {
                        var item = (DataViewItem)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);
                        if (item != null)
                        {
                            DragDrop.DoDragDrop(listViewItem, item, DragDropEffects.Move);
                        }
                    }
                }
            }
        }
    }

    private void SelectedListView_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void SelectedListView_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(DataViewItem)))
        {
            var droppedData = e.Data.GetData(typeof(DataViewItem)) as DataViewItem;
            var listView = sender as ListView;
            var target = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);

            if (listView != null && droppedData != null)
            {
                var items = listView.ItemsSource as ObservableCollection<DataViewItem>;
                if (items == null) return;

                int oldIndex = items.IndexOf(droppedData);
                int newIndex = items.Count - 1;

                if (target != null)
                {
                    var targetData = (DataViewItem)listView.ItemContainerGenerator.ItemFromContainer(target);
                    newIndex = items.IndexOf(targetData);
                }

                if (oldIndex != newIndex && oldIndex >= 0 && newIndex >= 0)
                {
                    items.Move(oldIndex, newIndex);
                }
            }
        }
    }

    // Helper to find ancestor of a type
    private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T)
            {
                return (T)current;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
