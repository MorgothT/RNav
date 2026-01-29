using Mapper_v1.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mapper_v1.Views
{
    /// <summary>
    /// Interaction logic for TerminalDialogPage.xaml
    /// </summary>
    public partial class TerminalDialogPage : Page
    {
        public TerminalDialogPage(TerminalViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
            this.Unloaded += TerminalDialogPage_Unloaded;
        }

        private async void TerminalDialogPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is TerminalViewModel vm)
            {
                //vm.TerminalText.Clear();
                await vm.DisconnectAsync();
            }
        }

        //private void ListBox_TargetUpdated(object sender, DataTransferEventArgs e)
        //{
        //    if (sender is ListBox listBox && listBox.Items.Count > 0)
        //    {
        //        var lastItem = listBox.Items[listBox.Items.Count - 1];
        //        App.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
        //        {
        //            listBox.ScrollIntoView(lastItem);
        //        }));
                
        //    }
        //}

        //private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    if (sender is TextBox tb)
        //    {
        //        tb.ScrollToEnd();
        //        //tb.CaretIndex = tb.Text.Length;
        //    }
        //}
    }
}
