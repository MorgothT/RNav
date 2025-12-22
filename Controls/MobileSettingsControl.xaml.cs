using CommunityToolkit.Mvvm.Input;
using Mapper_v1.Core.Models;
using Mapper_v1.Core.Services;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Mapper_v1.Controls
{
    /// <summary>
    /// Interaction logic for MobileSettingsControl.xaml
    /// </summary>
    public partial class MobileSettingsControl : UserControl
    {
        public MobileSettingsControl()
        {
            InitializeComponent();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            string path = FileService.GetApplicationFolder();
            path = Path.Combine(path, @"current\VesselShapes");
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Boat Shape Files (*.shp)|*.shp|All Files (*.*)|*.*",
                Title = "Select Boat Shape File",
                CustomPlaces = 
                {
                    new Microsoft.Win32.FileDialogCustomPlace(path),
                    new Microsoft.Win32.FileDialogCustomPlace(@"C:\RNav") 
                },
                InitialDirectory = path
            };
            var result = ofd.ShowDialog();
            if (result.Value == true)
            {
                (DataContext as Mobile).MobileShapeSettings.ShapePath = ofd.FileName;
            }
        }
    }
}
