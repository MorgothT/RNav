using System.Windows.Controls;

using MahApps.Metro.Controls;

using Mapper_v1.Contracts.Views;
using Mapper_v1.ViewModels;

namespace Mapper_v1.Views;

public partial class ShellWindow : MetroWindow, IShellWindow
{
    public ShellWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public Frame GetNavigationFrame()
        => shellFrame;

    public void ShowWindow()
        => Show();

    public void CloseWindow()
        => Close();
}
