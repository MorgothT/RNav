using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapper_v1.Contracts.Services;
using Mapper_v1.Services;
using System.Windows.Input;

namespace Mapper_v1.ViewModels;

public class ShellDialogViewModel : ObservableObject
{
    private ICommand _closeCommand;
    public ICommand CloseCommand => _closeCommand ?? (_closeCommand = new RelayCommand(OnClose));
    public Action<bool?> SetResult { get; set; }
    
    //private IConfigService _configService;

    public ShellDialogViewModel()//IConfigService configService)
    {
        //_configService = configService;
    }

    private void OnClose()
    {
        //_configService.Save();
        bool result = true;
        SetResult(result);
    }
}
