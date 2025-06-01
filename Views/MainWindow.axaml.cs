using System;
using System.Reactive.Disposables;
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
    }

     
}