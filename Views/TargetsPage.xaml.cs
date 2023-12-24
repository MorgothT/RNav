using Mapper_v1.ViewModels;
using System.Windows.Controls;

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
}
