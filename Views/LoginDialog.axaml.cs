using System;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using CumbyMinerScan.ViewModels;
using ReactiveUI;

namespace CumbyMinerScan.Views;

public partial class LoginDialog : ReactiveWindow<LoginDialogViewModel>
{
    public bool? Result { get; private set; }

    public LoginDialog()
    {
        InitializeComponent();
        if (DataContext is LoginDialogViewModel vm)
        {
            vm.CloseAction = (confirmed, username, password) =>
            {
                // 关闭窗口时传递一个元组或自定义类型对象
                this.Close((object)(confirmed, username, password));
            };
        }
    }

    public LoginDialog(LoginDialogViewModel vm) : this()
    {
        DataContext = vm;

        vm.CloseAction = (confirmed, username, password) =>
        {
            // 关闭窗口时传递一个元组或自定义类型对象
            this.Close((object)(confirmed, username, password));
        };
    }
}