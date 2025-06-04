using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

public static class HttpHelper
{
    private static readonly HttpClient _httpClient = new HttpClient();

    // 单个请求
    public static async Task<string> GetHtmlAsync(string url)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // 添加请求头
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:139.0) Gecko/20100101 Firefox/139.0");
            request.Headers.Add("Accept", "text/plain, */*; q=0.01");
            request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
            request.Headers.Add("Accept-Encoding", "gzip, deflate");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Add("Authorization", @"Digest username=""root"", realm=""antMiner Configuration"", nonce=""721eb1846cf24c9a26cfc87d8177ed86"", uri=""/cgi-bin/hlog.cgi"", response=""666676902530643c71bb06be407549f1"", qop=auth, nc=00005b9e, cnonce=""51e42da80eb31a2f""");
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
}