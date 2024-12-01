using System.Net.Http;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Web.WebView2.Core;
using System.Diagnostics;
using System.Text.Json;
using PullReqInfoCollector.Data.PullRequests;
using System.IO;
using PullReqInfoCollector.Data.PullRequestsThreads;
using System.Collections.Concurrent;
using System.CodeDom;
using System.Xml.Linq;

namespace PullReqInfoCollector;

public partial class MainWindow : Window
{
    private string OrganizatioinName = "tera1707";
    private string ProjectName = "TeraPrivateProject";
    private string RepositoryName = "TeraPrivateProject";

    private PullRequestsInfo? pullRequestsInfo;
    //ConcurrentQueue<int> pullRequestIds = new ConcurrentQueue<int>();

    List<DisplayPullRequestData> prDatas = new();
    List<DisplayThreadData> threadDatas = new();


    int KindFlag = 0;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        tbOrganizationName.Text = "tera1707";
        tbProjectName.Text = "TeraPrivateProject";
        tbRepositoryName.Text = "TeraPrivateProject";

        InitwebView();
    }

    private void InitwebView()
    {
        webView2.CoreWebView2InitializationCompleted += ((sender, e) =>
        {
            webView2.CoreWebView2.WebResourceResponseReceived += CoreWebView2_WebResourceResponseReceived;
        });

        webView2.EnsureCoreWebView2Async(null).GetAwaiter();
    }

    private async void CoreWebView2_WebResourceResponseReceived(object sender, CoreWebView2WebResourceResponseReceivedEventArgs e)
    {
        try
        {
            var stream = await e.Response.GetContentAsync();

            if (KindFlag == 0)
            {
                // https://learn.microsoft.com/ja-jp/rest/api/azure/devops/git/pull-requests/get-pull-requests?view=azure-devops-rest-7.1&tabs=HTTP

                GetResult<PullRequestsInfo>(stream, (pr) =>
                {
                    Debug.WriteLine($"プルリク件数：{pr.count} 件");

                    var PRs = pr.value;

                    PRs.ToList().ForEach(x =>
                    {
                        //pullRequestIds.Enqueue(x.pullRequestId);//★
                        Debug.WriteLine($"pullRequestId = {x.pullRequestId}, status = {x.status}, createBy = {x.createdBy.displayName}, title={x.title}, 作成日={x.creationDate}, 作成日からの期間={(DateTime.Now - x.creationDate).Days}日");
                        
                        prDatas.Add(new DisplayPullRequestData(x.repository.name, x.pullRequestId, x.title, x.createdBy.uniqueName, (DateTime.Now - x.creationDate)));
                    });

                    Debug.WriteLine("---");

                    pullRequestsInfo = pr;
                });
            }
            else if (KindFlag == 1)
            {
                // https://learn.microsoft.com/ja-jp/rest/api/azure/devops/git/pull-request-threads/list?view=azure-devops-rest-7.1&tabs=HTTP


                GetResult<PullRequestsThreadsInfo>(stream, (threads) =>
                {
                    var unresolvedCommentCount = threads.value.Where(x => x.status != "fixed").Count();
                    var ResolvedCommentCount = threads.value.Where(x => x.status == "fixed").Count();

                    Debug.Write("■スレッド情報  ");
                    Debug.WriteLine($"件数：{threads.count} 件, active：{unresolvedCommentCount}, Resolved：{ResolvedCommentCount}");

                    var threadCount = threads.count;

                    for ( int i = 0; i < threadCount; i++ )
                    {
                        var thread = threads.value[i];
                        var commentCount = thread.comments.Count();

                        Debug.WriteLine($"スレッド[{i}] 状態：{thread.status}, 作成者：{thread.comments[0].author.uniqueName}, ID={thread._links}");

                        if (thread.comments.Count() == 0)
                            continue;

                        var href = thread.comments.First()._links.self.href;
                        var prUrl = GetPullReqUrlFromPullReqRefString(href);
                        var prId = GetPullReqIdFromPullReqRefString(href);
                        threadDatas.Add(new DisplayThreadData(prId, thread.comments.First().content, prUrl));

                        //for ( int j = 0; j < commentCount; j++ )
                        //{
                        //    var comment = thread.comments[j];
                        //    var prUrl = GetPullReqUrlFromPullReqRefString(comment._links.self.href);
                        //    Debug.WriteLine($" コメントした人：{comment.author.uniqueName}({comment.author.displayName}), {comment.content}, プルリクURL：{prUrl}");
                        //}                         
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private string GetPullReqUrlFromPullReqRefString(string prReq)
    {
        var parts = prReq.Split('/');

        // https://dev.azure.com/tera1707/_apis/git/repositories/ec19976b-a797-4184-9c1b-22d5e9d2e837/pullRequests/3/threads/4/comments/1
        // というリンクの後ろから5つ目がプルリクID、一番最後がスレッドID
        var prId = int.Parse(parts[parts.Count() - 5]);

        var prUrl = $"https://dev.azure.com/{OrganizatioinName}/{ProjectName}/_git/{RepositoryName}/pullRequest/{prId}";///thread/{threadId}";

        return prUrl;
    }

    private int GetPullReqIdFromPullReqRefString(string prReq)
    {
        var parts = prReq.Split('/');

        // https://dev.azure.com/tera1707/_apis/git/repositories/ec19976b-a797-4184-9c1b-22d5e9d2e837/pullRequests/3/threads/4/comments/1
        // というリンクの後ろから5つ目がプルリクID、一番最後がスレッドID
        var prId = int.Parse(parts[parts.Count() - 5]);

        return prId;
    }


    private void GetResult<T>(Stream? stream, Action<T> onResult)
    {
        StreamReader reader = new StreamReader(stream);
        string text = reader.ReadToEnd();

        var jsonObj = JsonSerializer.Deserialize<T>(text);

        onResult.Invoke(jsonObj);
    }

    private void GetPullRequestInfo()
    {
        OrganizatioinName = tbOrganizationName.Text;
        ProjectName = tbProjectName.Text;
        RepositoryName = tbRepositoryName.Text;

        KindFlag = 0;
        webView2.CoreWebView2.Navigate($"https://dev.azure.com/{OrganizatioinName}/{ProjectName}/_apis/git/repositories/{RepositoryName}/pullrequests?api-version=7.1-preview.1&searchCriteria.status=all");//全部
        //webView2.CoreWebView2.Navigate($"https://dev.azure.com/{OrganizatioinName}/{ProjectName}/_apis/git/repositories/{RepositoryName}/pullrequests?api-version=7.1-preview.1");//Activeだけ
    }

    private async Task GetThreadsInfo()
    {
        KindFlag = 1;

        Debug.WriteLine($"プルリク残件数：{prDatas.Count()}");

        var prCouont = prDatas.Count();

        for (int i = 0; i < prCouont; i++)
        {
            var prId = prDatas[i].PullRequestId;

            //Debug.WriteLine($"prId = {prId}, i = {i}");

            webView2.CoreWebView2.Navigate($"https://dev.azure.com/{OrganizatioinName}/{ProjectName}/_apis/git/repositories/{RepositoryName}/pullRequests/{prId}/threads?api-version=7.1-preview.1");


            await Task.Delay(1000);
        }
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        prDatas.Clear();
        threadDatas.Clear();

        // 存在するプルリクを確認
        GetPullRequestInfo();

        // ほんとはシグナルで受信を待つが、めんどうなので一旦1秒待たせる
        await Task.Delay(1000);

        // プルリク内のスレッド(コメント)の情報を取る
        await GetThreadsInfo();

        // ここで、必要な情報が揃ってるはずなので、整理する

        Debug.WriteLine("");

        prDatas.ForEach(pr =>
        {
            threadDatas.Where(th => th.PullRequestId == pr.PullRequestId).ToList().ForEach(th => 
            {
                Debug.WriteLine($"★{pr.repositoryName}, {pr.Title}, {th.TextOfTopComment}, {th.PullRequestUrl}");
            });
        });


    }
}

/*
どういう情報を取りたい？

プルリクについた回答必要なコメントの情報
・スレッド作成者＝自分でActiveなスレッドで最終コメントが自分でないスレッド（人のプルリクにコメントして、回答が来てるもの）
・スレッド作成者≠自分で自分がコメントしていて最終コメントが自分でないスレッド（自分のプルリクのコメントに自分が回答して、回答待ちのもの）

プルリクの情報
→これは、azureの画面で見れるからいらんか。
*/

public record DisplayPullRequestData(string repositoryName, int PullRequestId, string Title, string CreatedBy, TimeSpan LiveDuration);

public record DisplayThreadData(int PullRequestId, string TextOfTopComment, string PullRequestUrl);

