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

namespace CumbyMinerScan.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public bool FanIssue { get; set; }
    public bool HashBoardIssue { get; set; }
    public bool PowerIssue { get; set; }
    public bool TempIssue { get; set; }
    public string RequestUsername { get; set; } = "root";
    public string RequestPassword { get; set; } = "root";
    public string RequestData { get; set; } = "{\"blink\":true}";
    public Interaction<Unit, string?> ShowFileDialog { get; }
    private string _inputText;
    private string _outputText;
    private string _messageText = "这里显示操作结果";
    public ReactiveCommand<Unit, Unit> OpenFileCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenLoginCommand { get; }

    public event Action? RequestLogin;

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

    public string MessageText
    {
        get => _messageText;
        set => this.RaiseAndSetIfChanged(ref _messageText, value);
    }

    public ObservableCollection<TableItem> TableData { get; } = new ObservableCollection<TableItem>();

    // 原始所有数据
    public ObservableCollection<DataRowViewModel> TableDataOrigin { get; } = new();

    // 筛选后的数据 一直为0
    private List<DataRowViewModel> _filteredP0Rows = new();

    public List<DataRowViewModel> FilteredP0Rows => _filteredP0Rows;

    // 筛选后的数据 瞬时为0
    private List<DataRowViewModel> _filteredT0Rows = new();
    public List<DataRowViewModel> FilteredT0Rows => _filteredT0Rows;

    public ObservableCollection<DataRowViewModel> TableDataMiner
    {
        get => _tableData;
        set => this.RaiseAndSetIfChanged(ref _tableData, value);
    }

    // 配置矿机的错误码，问题归类，解决意见
    public ICommand IssueConfigCommand { get; }

    public ICommand TestCommand { get; }

    // 过滤瞬时为0但平均不为0的矿机
    public ICommand FilterT0Command { get; }
    public ICommand FilterP0Command { get; }
    public ICommand SelectCommand { get; }
    public ICommand DetectIssueCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand RebootCommand { get; }
    public ICommand LightOnCommand { get; }
    public ICommand MessageCommand { get; }
    public ICommand SettingCommand { get; }
    public ICommand ListFanCommand { get; }
    public Interaction<Unit, (bool confirmed, string username, string password)> ShowLoginDialog { get; } = new();

    private List<string> GetIPList()
    {
        var ips = _outputText
            .Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(ip => ip.Trim())
            .Where(ip => IPAddress.TryParse(ip, out _)) // 只保留合法IP
            .ToList();
        return ips;
    }

    public MainWindowViewModel()
    {
        ExportCommand = ReactiveCommand.Create(async () =>
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"miner_data_{timestamp}.csv";
                CsvExporter.ExportToCsv(TableDataMiner.ToList(), fileName);
                Console.WriteLine("✅ 导出成功！");
                var box = MessageBoxManager
                    .GetMessageBoxStandard("Caption", $"{fileName}文件已经成功导出",
                        ButtonEnum.YesNo);

                var result = await box.ShowAsync();
                OpenFolderAndSelectFile(fileName);
            }
            catch (Exception ex)
            {
                // 记录错误或通知用户
                Console.WriteLine("导出时发生异常: " + ex.Message);
            }
        });
        // 设置用户名和密码
        SettingCommand = ReactiveCommand.Create(() => { });
        OpenLoginCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var (confirmed, username, password) = await ShowLoginDialog.Handle(Unit.Default);
            if (confirmed)
            {
                // 使用用户名密码
                MessageText = $"用户名:{username},密码:{password}";
                RequestUsername = username;
                RequestPassword = password;
                var result =
                    await HttpHelper.GetDigestProtectedResourceAsync("http://10.11.2.97/cgi-bin/blink.cgi",
                        username, password, RequestData);
                Console.WriteLine(result);
            }
            else
            {
                MessageText = $"取消登录 用户名:{username},密码:{password}";
            }
        });

        RebootCommand = ReactiveCommand.Create(() =>
        {
            // 重启列表中机器
        });
        TestCommand = ReactiveCommand.Create(() =>
        {
            var ips = GetIPList();
            
        });
        LightOnCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var ips = GetIPList();
            if (ips.Count == 0)
            {
                var box = MessageBoxManager
                    .GetMessageBoxStandard("Caption", $"IP列表为0，请重新填写",
                        ButtonEnum.YesNo);
            }
            else
            {
                var resultStrings = await HttpHelper.LightMinerList(ips);
                TableDataMiner.Clear();
                List<DataRowViewModel> dataRows = resultStrings.Select(row => new DataRowViewModel
                {
                    Cells = row
                }).ToList();
                foreach (var dataRow in dataRows)
                {
                    TableDataMiner.Add(dataRow);
                }
            }
        });
        MessageCommand = ReactiveCommand.Create(async () =>
        {
            var box = MessageBoxManager
                .GetMessageBoxStandard("Caption", "Are you sure you would like to delete appender_replace_page_1?",
                    ButtonEnum.YesNo);

            var result = await box.ShowAsync();
        });
        DetectIssueCommand = ReactiveCommand.Create(async () =>
        {
            MessageText = "开始检测0算计机器的问题";
            string prefix = "http://";
            string suffix = "/cgi-bin/hlog.cgi";
            var ips = _outputText
                .Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ip => ip.Trim()) // 清除前后空格
                .ToList();

            var urls = new List<string>();
            foreach (var ip in ips)
            {
                urls.Add("http://" + ip + "/cgi-bin/hlog.cgi");
            }
            // 调用并行请求方法，await不会阻塞UI线程

            List<string> htmlResults = await HttpHelper.GetHtmlListParallelAsync(urls);
            var box = MessageBoxManager
                .GetMessageBoxStandard("Caption", "所有IP已经解析完毕",
                    ButtonEnum.YesNo);
            var minerErrors = new List<DataRowViewModel>();
            // 处理结果，比如绑定到UI
            for (int i = 0; i < urls.Count; i++)
            {
                var errorMiner = LogHelper.ParseLog(ips[i], htmlResults[i]);
                minerErrors.Add(errorMiner);
            }

            TableDataMiner.Clear();
            foreach (var row in minerErrors)
            {
                TableDataMiner.Add(row);
            }

            MessageText = "检测0算计机器的问题完毕";
        });
        IssueConfigCommand = ReactiveCommand.Create(OnSubmit);
        SelectCommand = ReactiveCommand.Create(OnSelect);
        FilterT0Command = ReactiveCommand.Create(() =>
        {
            TableDataMiner.Clear();
            foreach (var row in _filteredT0Rows)
            {
                TableDataMiner.Add(row);
            }

            var ipList = TableDataMiner
                .Select(row => row.Cells[0].Trim('"')) // 取出 IP 并去掉前后空格
                .Where(ip => !string.IsNullOrEmpty(ip)) // 排除空值
                .ToList();

            OutputText = string.Join(Environment.NewLine, ipList); // 换行拼接
        });
        FilterP0Command = ReactiveCommand.Create(() =>
        {
            TableDataMiner.Clear();
            foreach (var row in _filteredP0Rows)
            {
                TableDataMiner.Add(row);
            }

            var ipList = TableDataMiner
                .Select(row => row.Cells[0].Trim('"')) // 取出 IP 并去掉前后空格
                .Where(ip => !string.IsNullOrEmpty(ip)) // 排除空值
                .ToList();

            OutputText = string.Join(Environment.NewLine, ipList); // 换行拼接
        });
        ShowFileDialog = new Interaction<Unit, string?>();
        OpenFileCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var filePath = await ShowFileDialog.Handle(Unit.Default);
            if (!string.IsNullOrEmpty(filePath))
            {
                OutputText = $"你选择的文件是：{filePath}";
                await Task.Run(() =>
                {
                    var newData = ReadTableDataFromFile(filePath);
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        TableDataMiner.Clear();
                        TableDataOrigin.Clear();
                        for (var i = 0; i < newData.Count; i++)
                        {
                            TableDataMiner.Add(newData[i]);
                            TableDataOrigin.Add(newData[i]);
                        }

                        FilterAverage0ZeroHashrateRows();
                        FilterTempZeroHashrateRows();
                        OutputText = $"你选择的文件是：{filePath}";
                    });
                });
            }
        });
    }

    // 获取瞬时为0
    private void FilterTempZeroHashrateRows()
    {
        _filteredT0Rows.Clear();
        foreach (var row in TableDataOrigin)
        {
            if (row.Cells.Count < 6) continue; // 防止越界
            // 清洗后尝试将第5、6列转换为数字
            string realStr = Regex.Replace(row.Cells[4], @"[^\d\.]", "").Trim();
            string avgStr = Regex.Replace(row.Cells[5], @"[^\d\.]", "").Trim();
            bool realParsed = float.TryParse(realStr.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture,
                out float realVal);
            bool avgParsed = float.TryParse(avgStr.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture,
                out float avgVal);
            if (realVal == 0)
            {
                _filteredT0Rows.Add(row);
            }
        }
    }

    // 获取平均为0
    private void FilterAverage0ZeroHashrateRows()
    {
        _filteredP0Rows.Clear();
        foreach (var row in TableDataOrigin)
        {
            if (row.Cells.Count < 6) continue; // 防止越界

            // 清洗后尝试将第5、6列转换为数字
            string realStr = Regex.Replace(row.Cells[4], @"[^\d\.]", "").Trim();
            string avgStr = Regex.Replace(row.Cells[5], @"[^\d\.]", "").Trim();
            bool realParsed = float.TryParse(realStr.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture,
                out float realVal);
            bool avgParsed = float.TryParse(avgStr.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture,
                out float avgVal);

            if (realVal == 0 && avgVal == 0)
            {
                _filteredP0Rows.Add(row);
            }
        }
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

    private ObservableCollection<DataRowViewModel> _tableData = new();


    private List<DataRowViewModel> ReadTableDataFromFile(string path)
    {
        var result = new List<DataRowViewModel>();
        using var reader = new StreamReader(path);
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (!string.IsNullOrWhiteSpace(line))
            {
                var cells = line.Split(',').ToList();
                result.Add(new DataRowViewModel { Cells = cells });
            }
        }

        return result;
    }

    public static void OpenFolderAndSelectFile(string relativePath)
    {
        string fullPath = Path.GetFullPath(relativePath);

        if (OperatingSystem.IsWindows() && File.Exists(fullPath))
        {
            var argument = $"/select,\"{fullPath}\"";
            Process.Start("explorer.exe", argument);
        }
    }
}