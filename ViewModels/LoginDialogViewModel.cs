using System;
using System.Windows.Input;
using ReactiveUI;

namespace CumbyMinerScan.ViewModels;

public class LoginDialogViewModel : ViewModelBase
{
    private string _username = "";
    private string _password = "";

    public string Username
    {
        get => _username;
        set => this.RaiseAndSetIfChanged(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    // 关闭窗口的接口
    public Action<bool>? CloseAction { get; set; }

    public ICommand OkCommand { get; }
    public ICommand CancelCommand { get; }

    public LoginDialogViewModel()
    {
        OkCommand = ReactiveCommand.Create(() => { CloseAction?.Invoke(true); });

        CancelCommand = ReactiveCommand.Create(() =>
        {
            Console.WriteLine("ok ok");
            CloseAction?.Invoke(false);
        });
    }
}