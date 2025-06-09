namespace CumbyMinerScan.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CumbyMinerScan.Models;
using CumbyMinerScan.Utils;
using CumbyMinerScan.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using ReactiveUI;

public class IssueConfigViewModel : ViewModelBase
{
    public ICommand CancelCommand { get; }
    public ICommand SaveCommand { get; }
    private ObservableCollection<IssueModel> _tableData = new();

    public ObservableCollection<IssueModel> IssueConfigData
    {
        get => _tableData;
        set => this.RaiseAndSetIfChanged(ref _tableData, value);
    }

    public IssueConfigViewModel()
    {
        
    }
}