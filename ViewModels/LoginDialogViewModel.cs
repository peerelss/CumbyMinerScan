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
    public Action<bool, string, string>? CloseAction { get; set; }

    public ICommand OkCommand { get; }
    public ICommand CancelCommand { get; }

    public LoginDialogViewModel()
    {
        OkCommand = ReactiveCommand.Create(() =>
        {
            Console.WriteLine($"登录 用户名:{_username},密码:{_password}");
            CloseAction?.Invoke(true, _username, _password);
        });

        CancelCommand = ReactiveCommand.Create(() =>
        {
            Console.WriteLine($"取消登录 用户名:{_username},密码:{_password}");
            CloseAction?.Invoke(false, "", "");
        });
    }
}