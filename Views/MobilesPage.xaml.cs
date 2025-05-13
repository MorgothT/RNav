using System.Windows.Controls;

using Mapper_v1.ViewModels;

namespace Mapper_v1.Views;

public partial class MobilesPage : Page
{
    public MobilesPage(MobilesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
