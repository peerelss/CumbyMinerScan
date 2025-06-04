using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CumbyMinerScan.Models;
using CumbyMinerScan.Utils;
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

    public ICommand SubmitCommand { get; }

    // 过滤瞬时为0但平均不为0的矿机
    public ICommand FilterT0Command { get; }
    public ICommand FilterP0Command { get; }
    public ICommand SelectCommand { get; }
    public ICommand TestCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand RebootCommand { get; }
    public ICommand LightOnCommand { get; }

    public MainWindowViewModel()
    {
        ExportCommand = ReactiveCommand.Create(() =>
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"miner_data_{timestamp}.csv";
                CsvExporter.ExportToCsv(TableDataMiner.ToList(), fileName);
                Console.WriteLine("✅ 导出成功！");
            }
            catch (Exception ex)
            {
                // 记录错误或通知用户
                Console.WriteLine("导出时发生异常: " + ex.Message);
            }
        });
        RebootCommand = ReactiveCommand.Create(() =>
        {
            // 重启列表中机器
        });
        LightOnCommand = ReactiveCommand.Create(() =>
        {
            // 点亮列表中机器
        });

        TestCommand = ReactiveCommand.Create(async () =>
        {
            string prefix = "http://";
            string suffix = "/cgi-bin/hlog.cgi";
            var ips = _outputText
                .Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ip => ip.Trim()) // 清除前后空格
                .ToList();
            foreach (var ip in ips)
            {
                Console.WriteLine($"http://{ip}/cgi-bin/hlog.cgi");
            }


            var urls = new List<string>();
            foreach (var ip in ips)
            {
                urls.Add("http://" + ip + "/cgi-bin/hlog.cgi");
            }

            // 调用并行请求方法，await不会阻塞UI线程
            foreach (var url in urls)
            {
                Console.WriteLine(url);
            }

            List<string> htmlResults = await HttpHelper.GetHtmlListParallelAsync(urls);
            var minerErrors = new List<DataRowViewModel>();
            // 处理结果，比如绑定到UI
            for (int i = 0; i < urls.Count; i++)
            {
                var errorMiner = LogHelper.ParseLog(ips[i], htmlResults[i]);
                minerErrors.Add(errorMiner);
            }

            for (int i = 0; i < minerErrors.Count; i++)
            {
                Console.WriteLine($"第{i}行: {string.Join(", ", minerErrors[i])}");
            }

            TableDataMiner.Clear();
            foreach (var row in minerErrors)
            {
                TableDataMiner.Add(row);
            }
        });
        SubmitCommand = ReactiveCommand.Create(OnSubmit);
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
            if (realVal == 0 && avgVal != 0)
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
}