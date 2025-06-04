using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CumbyMinerScan.Models;

public static class CsvExporter
{
    public static void ExportToCsv(List<DataRowViewModel> tableData, string filePath)
    {
        var sb = new StringBuilder();

        foreach (var row in tableData)
        {
            // 每个单元格加双引号，并用逗号连接
            var line = string.Join(",", row.Cells.ConvertAll(cell => $"\"{cell.Replace("\"", "\"\"")}\""));
            sb.AppendLine(line);
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }
}