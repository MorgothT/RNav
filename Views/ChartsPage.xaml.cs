using Mapper_v1.ViewModels;
using System.Windows.Controls;

namespace Mapper_v1.Views;

public partial class ChartsPage : Page
{
    public ChartsPage(ChartsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
