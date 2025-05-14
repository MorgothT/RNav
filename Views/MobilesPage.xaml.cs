using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Mapper_v1.Core.Models;
using Mapper_v1.ViewModels;

namespace Mapper_v1.Views;

public partial class MobilesPage : Page
{
    public MobilesPage(MobilesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private Point dragStartPoint;

    private void MobileItem_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void MobileList_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        dragStartPoint = e.GetPosition(null);
    }

    private void MobileList_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if( e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            Point position = e.GetPosition(null);
            if (Math.Abs(position.X - dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(position.Y - dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                if (sender is ListView listView)
                {
                    var listViewItem = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
                    if (listViewItem != null)
                    {
                        var mobile = (Mobile)listView.ItemContainerGenerator.ItemFromContainer(listViewItem);
                        if (mobile != null)
                        {
                            DragDrop.DoDragDrop(listViewItem, mobile, DragDropEffects.Move);
                        }
                    }
                }
            }
        }
    }

    private void MobileList_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(Mobile)))
        {
            var droppedData = e.Data.GetData(typeof(Mobile)) as Mobile;
            var listView = sender as ListView;
            var target = FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);

            if (listView != null && droppedData != null)
            {
                var mobiles = listView.ItemsSource as ObservableCollection<Mobile>;
                if (mobiles == null) return;

                int oldIndex = mobiles.IndexOf(droppedData);
                int newIndex = mobiles.Count - 1;

                if (target != null)
                {
                    var targetData = (Mobile)listView.ItemContainerGenerator.ItemFromContainer(target);
                    newIndex = mobiles.IndexOf(targetData);
                }

                if (oldIndex != newIndex && oldIndex >= 0 && newIndex >= 0)
                {
                    mobiles.Move(oldIndex, newIndex);
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
