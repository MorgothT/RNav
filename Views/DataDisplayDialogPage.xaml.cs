using System.Windows.Controls;

using Mapper_v1.ViewModels;

namespace Mapper_v1.Views;

public partial class DataDisplayDialogPage : Page
{
    public DataDisplayDialogPage(DataDisplayDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
