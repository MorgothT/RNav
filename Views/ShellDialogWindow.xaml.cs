using MahApps.Metro.Controls;
using Mapper_v1.Contracts.Views;
using Mapper_v1.ViewModels;
using System.Windows.Controls;

namespace Mapper_v1.Views;

public partial class ShellDialogWindow : MetroWindow, IShellDialogWindow
{
    public ShellDialogWindow(ShellDialogViewModel viewModel)
    {
        InitializeComponent();
        viewModel.SetResult = OnSetResult;
        DataContext = viewModel;
    }

    public Frame GetDialogFrame()
        => dialogFrame;

    private void OnSetResult(bool? result)
    {
        DialogResult = result;
        Close();
    }
}
