using System.Windows.Controls;
using Mapper_v1.Models;
using Mapper_v1.ViewModels;
using Mapsui.Styles;

namespace Mapper_v1.Views;

public partial class ChartsPage : Page
{
    public ChartsPage(ChartsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
