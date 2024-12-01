using PullReqInfoCollector.Data.PullRequests;
using PullReqInfoCollector.Data.PullRequestsThreads;
using PullReqInfoCollector.Model;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace PullReqInfoCollector;

public partial class MainWindow : Window
{
    private string OrganizatioinName = "tera1707";
    private string ProjectName = "TeraPrivateProject";
    private string RepositoryName = "TeraPrivateProject";
    private string SelfMailAddr = "tera1707@gmail.com";

    WebAccess? wa;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        tbOrganizationName.Text = "tera1707";
        tbProjectName.Text = "TeraPrivateProject";
        tbRepositoryName.Text = "TeraPrivateProject";
        tbSelfMailAddr.Text = "tera1707@gmail.com";

        wa = new WebAccess(webView2);
        await wa.Initialize($"https://dev.azure.com/{OrganizatioinName}/{ProjectName}/_apis/git/repositories/{RepositoryName}/pullrequests?api-version=7.1-preview.1&searchCriteria.status=all");

        btGetInfo.IsEnabled = false;
        await Task.Delay(1000);
        btGetInfo.IsEnabled = true;
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        await GetInformation();
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

    private async Task GetInformation()
    {
        OrganizatioinName = tbOrganizationName.Text;
        ProjectName = tbProjectName.Text;
        RepositoryName = tbRepositoryName.Text;

        var prInfo = await wa.GetContentAsync<PullRequestsInfo>($"https://dev.azure.com/{OrganizatioinName}/{ProjectName}/_apis/git/repositories/{RepositoryName}/pullrequests?api-version=7.1-preview.1&searchCriteria.status=all");

        if (prInfo is null)
        {
            Debug.WriteLine("プルリク情報がありませんでした。");
            return;
        }

        var ctr = prInfo.value.Count();

        for (int i = 0; i < ctr; i++)
        {
            var thInfo = await wa.GetContentAsync<PullRequestsThreadsInfo>($"https://dev.azure.com/{OrganizatioinName}/{ProjectName}/_apis/git/repositories/{RepositoryName}/pullRequests/{prInfo.value[i].pullRequestId}/threads?api-version=7.1-preview.1");

            prInfo.value[i].repository.threadsInfo = thInfo;
        }


        for (int i = 0; i < prInfo.count; i++)
        {
            var pr = prInfo.value[i];
            var repo = pr.repository;
            var threads = repo.threadsInfo;
            var threadsCount = threads.count;
            var prUrl = "";

            SelfMailAddr = tbSelfMailAddr.Text;

            for (int j = 0; j < threadsCount; j++)
            {
                var thread = threads.value[j];

                if (thread.comments is null || thread.comments.Length == 0)
                    continue;

                var href = thread.comments.First()._links.self.href;
                prUrl = GetPullReqUrlFromPullReqRefString(href);
                //var prId = GetPullReqIdFromPullReqRefString(href);

                var txtThread = "";
                if ((thread.status == "active" && thread.comments.First().author.uniqueName == SelfMailAddr && thread.comments.Last().author.uniqueName != SelfMailAddr)
                    // スレッド作成者＝自分でActiveなスレッドで最終コメントが自分でないスレッド（人のプルリクにコメントして、回答が来てるもの）
                    || (thread.status == "active" && thread.comments.First().author.uniqueName != SelfMailAddr && thread.comments.Last().author.uniqueName != SelfMailAddr && thread.comments.Any(x => x.author.uniqueName == SelfMailAddr)))
                    // スレッド作成者≠自分で自分がコメントしていて最終コメントが自分でないスレッド（自分のプルリクのコメントに自分が回答して、回答待ちのもの）
                {
                    txtThread = "〇 ";
                }
                else
                {
                    txtThread = "× ";
                }
                txtThread += $"タイトル：{pr.title} 先頭コメント：{thread.comments.First().content.ToString().Replace("\n", "")}, st：{thread.status}, 先頭コメント者：{thread.comments.First().author.uniqueName}, 最後尾コメント者：{thread.comments.Last().author.uniqueName}";
                //Debug.Write($"pullRequestId = {pr.pullRequestId}, プルリクst = {pr.status}, createBy = {pr.createdBy.displayName}, title={pr.title}, 作成日={pr.creationDate}, 生存期間={(DateTime.Now - pr.creationDate).Days}日, コメント数：{threadsCount}");
                Debug.WriteLine(txtThread);
                lbThread.Items.Add(new DisplayData(txtThread, prUrl));
            }

            var target = (pr.createdBy.uniqueName == SelfMailAddr) ? "〇" : "×";
            var txtPr = $"{target} タイトル：{pr.title}, st：{pr.status}, createBy：{pr.createdBy.displayName}, 作成日：{pr.creationDate.ToString("yyyy/MM/dd")}, 生存期間：{(DateTime.Now - pr.creationDate).Days}日, コメント数：{threadsCount}";
            Debug.WriteLine(txtPr);
            lbPullRequest.Items.Add(new DisplayData(txtPr, prUrl));
        }
    }

    private record DisplayData(string DisplayString, string Url);


    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        var si = new ProcessStartInfo(e.Uri.AbsoluteUri);
        si.UseShellExecute = true;
        Process.Start(si);
        e.Handled = true;
    }
}
