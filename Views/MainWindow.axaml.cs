using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using CumbyMinerScan.ViewModels;
using ReactiveUI;

namespace CumbyMinerScan.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            ViewModel!.ShowFileDialog.RegisterHandler(async interaction =>
            {
                var dialog = new OpenFileDialog
                {
                    AllowMultiple = false,
                    Filters =
                    {
                        new FileDialogFilter { Name = "CSV", Extensions = { "csv" } },
                        new FileDialogFilter { Name = "Excel", Extensions = { "xls", "xlsx" } }
                    }
                };

                var result = await dialog.ShowAsync(this);
                interaction.SetOutput(result?.Length > 0 ? result[0] : null);
            }).DisposeWith(disposables);
        });
        this.WhenActivated(disposables =>
        {
            ViewModel!.ShowLoginDialog.RegisterHandler(async interaction =>
            {
                var dialog = new LoginDialog(); // 你自定义的对话框窗口
                var result = await dialog.ShowDialog<(bool, string, string)>(this);
                interaction.SetOutput(result);
            }).DisposeWith(disposables);
        });
        
    }
    
    
     
}