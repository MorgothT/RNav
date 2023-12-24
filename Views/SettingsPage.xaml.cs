using Mapper_v1.ViewModels;
using System.Windows.Controls;

namespace Mapper_v1.Views;

public partial class SettingsPage : Page
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
