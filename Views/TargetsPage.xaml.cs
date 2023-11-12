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
