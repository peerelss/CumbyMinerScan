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


        return fallbackPatterns;
    }

    public static DataRowViewModel ParseLog(string ip, string hlogStr)

    {
        string[] logList = hlogStr.Split('\n');
        // 定义错误模式及对应标签，元组：(匹配方式, 模式字符串, 标签)
        var errorPatterns = LoadErrorPatterns();
        // 倒叙，方便定位问题
        foreach (var logStrRaw in logList.Reverse())
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