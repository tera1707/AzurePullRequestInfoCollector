using Microsoft.Web.WebView2.Wpf;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace PullReqInfoCollector.Model;

internal class WebAccess : IDisposable
{
    private WebView2 _webView2;

    // _webView2.CoreWebView2.Navigate(url); した後、
    // WebResourceResponseReceivedが来て、e.Response.GetContentAsync()が終わるまでの待ちのためのイベント
    private ManualResetEvent manualEvent;

    private string? returnObj = null;

    public WebAccess(WebView2 webView2)
    {
        _webView2 = webView2;

    }

    public async Task Initialize(string initPage)
    {
        manualEvent = new ManualResetEvent(false);
        _webView2.Source = new Uri(initPage);


        _webView2.CoreWebView2InitializationCompleted += ((sender, e) =>
        {
            _webView2.CoreWebView2.WebResourceResponseReceived += (async (s, e) =>
            {
                try
                {
                    var stream = await e.Response.GetContentAsync();
                    returnObj = GetResult(stream);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                finally
                {
                    manualEvent.Set();
                }
            });
        });
        await _webView2.EnsureCoreWebView2Async(null);
    }

    public async Task<T?> GetContentAsync<T>(string url)
    {
        manualEvent.Reset();

        // 読み込み開始
        _webView2.CoreWebView2.Navigate(url);
        
        // 読み込み終了待ち(10秒でタイムアウト)
        await Task.Run(() => { manualEvent.WaitOne(TimeSpan.FromSeconds(10)); });        

        return JsonSerializer.Deserialize<T>(returnObj!);
    }

    private string GetResult(Stream? stream)
    {
        StreamReader reader = new StreamReader(stream!);
        string text = reader.ReadToEnd();

        //var jsonObj = JsonSerializer.Deserialize<T>(text);
        return text;
    }

    public void Dispose()
    {
        _webView2 = null;
        manualEvent.Dispose();
        manualEvent = null;
    }
}
