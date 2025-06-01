using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CumbyMinerScan.Models;
using ReactiveUI;

namespace CumbyMinerScan.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public Interaction<Unit, string?> ShowFileDialog { get; }
    private string _inputText;
    private string _outputText;
    public ReactiveCommand<Unit, Unit> OpenFileCommand { get; }

    public string InputText
    {
        get => _inputText;
        set => this.RaiseAndSetIfChanged(ref _inputText, value);
    }

    public string OutputText
    {
        get => _outputText;
        set => this.RaiseAndSetIfChanged(ref _outputText, value);
    }

    public ObservableCollection<TableItem> TableData { get; } = new ObservableCollection<TableItem>();

    public ICommand SubmitCommand { get; }
    public ICommand SelectCommand { get; }

    public MainWindowViewModel()
    {
        SubmitCommand = ReactiveCommand.Create(OnSubmit);
        SelectCommand = ReactiveCommand.Create(OnSelect);
        ShowFileDialog = new Interaction<Unit, string?>();
        OpenFileCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var filePath = await ShowFileDialog.Handle(Unit.Default);
            if (!string.IsNullOrEmpty(filePath))
            {
                OutputText = $"你选择的文件是：{filePath}";
            }
        });
    }

    private async void OnSelect()
    {
        OutputText = $"选择文件";
    }

    private void OnSubmit()
    {
        OutputText = $"你输入了: {InputText}";
        TableData.Add(new TableItem { Column1 = InputText, Column2 = "已提交" });
        InputText = string.Empty;
    }

    public class TableItem
    {
        public string Column1 { get; set; }
        public string Column2 { get; set; }
    }
}