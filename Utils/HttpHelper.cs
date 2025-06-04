using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public static class HttpHelper
{
    private static string _userName = "root";

    private static string _userPassword = "root";

    private static string _httpPre = "http://";

    //重启矿机
    private static string _reboot = "/cgi-bin/reboot.cgi";

    //点亮矿机
    private static string _lightMiner = "/cgi-bin/blink.cgi";
    private static string _lightData = "{\"blink\":true}";
    private static readonly HttpClient _httpClient = new HttpClient();

    // 单个请求
    public static async Task<string> GetHtmlAsync(string url)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // 添加请求头
            request.Headers.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:139.0) Gecko/20100101 Firefox/139.0");
            request.Headers.Add("Accept", "text/plain, */*; q=0.01");
            request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
            request.Headers.Add("Accept-Encoding", "gzip, deflate");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("Authorization",
                @"Digest username=""root"", realm=""antMiner Configuration"", nonce=""721eb1846cf24c9a26cfc87d8177ed86"", uri=""/cgi-bin/hlog.cgi"", response=""666676902530643c71bb06be407549f1"", qop=auth, nc=00005b9e, cnonce=""51e42da80eb31a2f""");
            request.Headers.Add("Connection", "keep-alive");
            request.Headers.Add("Referer", "http://10.21.1.113/");
            request.Headers.Add("Priority", "u=0");
            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();

            return content;
        }
        catch (Exception ex)
        {
            // 捕获异常，返回异常信息字符串，也可以改成返回null或者空字符串
            return $"请求失败：{ex.Message}";
        }
    }

    // 并行请求多个URL
    public static async Task<List<string>> GetHtmlListParallelAsync(List<string> urls)
    {
        // 为每个URL启动请求任务
        var tasks = new List<Task<string>>();

        foreach (var url in urls)
        {
            tasks.Add(GetHtmlAsync(url));
        }

        // 等待所有任务完成
        string[] results = await Task.WhenAll(tasks);

        // 转换成List返回
        return new List<string>(results);
    }

    public static async Task<string> LightMiner(string ip)
    {
        var resultContent =
            await GetDigestProtectedResourceAsync(_httpPre + ip + _lightMiner, _userName, _userPassword, _lightData);
        return resultContent;
    }

    public static async Task<List<List<string>>> LightMinerList(List<string> ipList)
    {
        var tasks = ipList.Select(async ip =>
        {
            try
            {
                var json = await GetDigestProtectedResourceAsync(
                    _httpPre + ip + _lightMiner, _userName, _userPassword, _lightData);

                using var doc = JsonDocument.Parse(json);
                var code = doc.RootElement.GetProperty("code").GetString();

                if (code == "B000")
                {
                    return new List<string> { ip, "success" };
                }

                else
                {
                    return new List<string> { ip, $"failure: code={code}" };
                }
            }
            catch (Exception ex)
            {
                return new List<string> { ip, $"failure: Message={ex.Message}" };
            }
        });

        return (await Task.WhenAll(tasks)).ToList();
    }

    public static async Task<string> GetDigestProtectedResourceAsync(string url, string username, string password,
        string payload)
    {
        var handler = new HttpClientHandler
        {
            PreAuthenticate = true,
            Credentials = new CredentialCache
            {
                {
                    new Uri(url),
                    "Digest",
                    new NetworkCredential(username, password)
                }
            }
        };

        using var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json") // 也可以是 text/plain 等
        };

        // 可选：加一些头
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0");
        request.Headers.Accept.ParseAdd("application/json");

        HttpResponseMessage response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"请求失败，状态码: {response.StatusCode}");
        }

        return await response.Content.ReadAsStringAsync();
    }
}