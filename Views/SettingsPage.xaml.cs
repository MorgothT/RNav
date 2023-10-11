using System.Windows.Controls;
using Mapper_v1.Models;
using Mapper_v1.ViewModels;

namespace Mapper_v1.Views;

public partial class SettingsPage : Page
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
