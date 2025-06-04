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
            vm.CloseAction = CloseDialog;
        }
    }

    private void CloseDialog(bool result)
    {
        Result = result;
        Close();
    }
    
}