using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CumbyMinerScan.Models;

namespace CumbyMinerScan.Utils;

public static class LogHelper
{
    public static List<(string method, string pattern, string label)> LoadErrorPatterns()
    {
        var fallbackPatterns = new List<(string, string, string)>
        {
            ("endswith", "asic, times 2", "miss asci"),
            ("in", "Not enough chain", "Not enough chain"),
            ("endswith", "ERROR_POWER_LOST: power voltage rise or drop, pls check!", "ERROR_POWER_LOST"),
            ("in", "ERROR_TEMP_TOO_HIGH", "ERROR_TEMP_TOO_HIGH"),
            ("in", "ERROR_FAN_LOST", "ERROR_FAN_LOST"),
            ("endswith", "nonce crc error", "nonce crc error"),
        };

        var filePath = "error_patterns.csv";

        var result = new List<(string, string, string)>();

        try
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException();

            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                // 跳过空行或标题行
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("method"))
                    continue;

                var parts = line.Split(',');

                if (parts.Length >= 3)
                {
                    string method = parts[0].Trim();
                    string pattern = parts[1].Trim();
                    string label = parts[2].Trim();

                    result.Add((method, pattern, label));
                }
            }

            if (result.Count == 0)
            {
                throw new Exception("CSV内容为空或格式不对");
            }


            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"no such file use default config: {ex.Message}");
            return fallbackPatterns;
        }
    }

    public static DataRowViewModel ParseLog(string ip, string hlogStr)

    {
        string[] logList = hlogStr.Split('\n');
        // 定义错误模式及对应标签，元组：(匹配方式, 模式字符串, 标签)
        var errorPatterns = LoadErrorPatterns();

        foreach (var logStrRaw in logList)
        {
            string logStr = logStrRaw ?? "";

            foreach (var (method, pattern, label) in errorPatterns)
            {
                if ((method == "endswith" && logStr.EndsWith(pattern)) ||
                    (method == "in" && logStr.Contains(pattern)))
                {
                    // 取日志开头的日期部分，假设用空格分割第一个元素
                    string happenDate = "";
                    var parts = logStr.Split(' ');
                    if (parts.Length > 0)
                    {
                        happenDate = parts[0];
                    }

                    Console.WriteLine($"{ip} {happenDate} {label} {logStr}");

                    return new DataRowViewModel
                    {
                        Cells = new List<string> { ip, happenDate, label, logStr }
                    };
                }
            }
        }

        // 未匹配任何错误时返回
        return new DataRowViewModel
        {
            Cells = new List<string> { ip, "unknown" }
        };
        ;
    }
}