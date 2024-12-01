using PullReqInfoCollector.Data.PullRequests;
using PullReqInfoCollector.Data.PullRequestsThreads;
using PullReqInfoCollector.Model;
using System.Diagnostics;
using System.Security.Policy;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace PullReqInfoCollector;

public partial class MainWindow : Window
{
    private string OrganizatioinName = "tera1707";
    private string ProjectName = "TeraPrivateProject";
    //private string RepositoryName = "TeraPrivateProject";
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
        //tbRepositoryName.Text = "TeraPrivateProject";
        tbSelfMailAddr.Text = "tera1707@gmail.com";

        wa = new WebAccess(webView2);
        await wa.Initialize($"https://dev.azure.com/{OrganizatioinName}/{ProjectName}/_apis/git/repositories?api-version=7.1-preview.1&searchCriteria.status=all");

        btGetInfo.IsEnabled = false;
        await Task.Delay(1000);
        btGetInfo.IsEnabled = true;
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        await GetInformation();
    }

    private string GetPullReqUrlFromPullReqRefString(string repoName, string prReq)
    {
        var parts = prReq.Split('/');

        // https://dev.azure.com/tera1707/_apis/git/repositories/ec19976b-a797-4184-9c1b-22d5e9d2e837/pullRequests/3/threads/4/comments/1
        // というリンクの後ろから5つ目がプルリクID、一番最後がスレッドID
        var prId = int.Parse(parts[parts.Count() - 5]);

        var prUrl = $"https://dev.azure.com/{OrganizatioinName}/{ProjectName}/_git/{repoName}/pullRequest/{prId}";///thread/{threadId}";

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
        //RepositoryName = tbRepositoryName.Text;
        SelfMailAddr = tbSelfMailAddr.Text;

        // 一旦表示をクリア
        lbPullRequest.Items.Clear();
        lbThread.Items.Clear();

        var repoInfo = await wa.GetContentAsync<RepositoryInfo>($"https://dev.azure.com/{OrganizatioinName}/{ProjectName}/_apis/git/repositories?api-version=7.1-preview.1&searchCriteria.status=all");

        if (repoInfo is null)
        {
            Debug.WriteLine("リポジトリ情報がありませんでした。");
            return;
        }

        var repoCtr = repoInfo.count;

        // projectがリポジトリ名っぽい

        PullRequestsInfo? prInfo;

        for (int k = 0; k < repoCtr; k++)//k:リポジトリ番号
        {
            var repo = repoInfo.value[k];

            // リポジトリ毎の全プルリク情報
            prInfo = await wa.GetContentAsync<PullRequestsInfo>($"https://dev.azure.com/{OrganizatioinName}/{ProjectName}/_apis/git/repositories/{repo.name}/pullrequests?api-version=7.1-preview.1&searchCriteria.status=all");

            repo.project.PullRequests = prInfo;

            var ctr = prInfo.value.Count();

            for (int l = 0; l < ctr; l++)//l:プルリク番号
            {
                var prUrl = "";
                var pr = repo.project.PullRequests.value[l];

                // プルリク毎の全スレッド情報
                var thInfo = await wa.GetContentAsync<PullRequestsThreadsInfo>($"https://dev.azure.com/{OrganizatioinName}/{ProjectName}/_apis/git/repositories/{repo.name}/pullRequests/{pr.pullRequestId}/threads?api-version=7.1-preview.1");

                pr.threadsInfo = thInfo;
                var threadCount = pr.threadsInfo.value.Count();

                for (int j = 0; j < threadCount; j++)
                {
                    var thread = pr.threadsInfo.value[j];

                    if (thread.comments is null || thread.comments.Length == 0)
                        continue;

                    var href = thread.comments.First()._links.self.href;
                    prUrl = GetPullReqUrlFromPullReqRefString(repo.name, href);
                    //var prId = GetPullReqIdFromPullReqRefString(href);

                    ///////////////////////////////////
                    // スレッド(コメント情報の表示)
                    ///////////////////////////////////
                    var txtThread = $"{repo.name}, {pr.title} 先頭：{thread.comments.First().content.ToString().Replace("\n", "")}, st：{thread.status}, 先頭者：{thread.comments.First().author.uniqueName}, 末尾者：{thread.comments.Last().author.uniqueName}";

                    if ((thread.status == "active" && thread.comments.First().author.uniqueName == SelfMailAddr && thread.comments.Last().author.uniqueName != SelfMailAddr)
                        // スレッド作成者＝自分でActiveなスレッドで最終コメントが自分でないスレッド（人のプルリクにコメントして、回答が来てるもの）
                        || (thread.status == "active" && thread.comments.First().author.uniqueName != SelfMailAddr && thread.comments.Last().author.uniqueName != SelfMailAddr && thread.comments.Any(x => x.author.uniqueName == SelfMailAddr)))
                    // スレッド作成者≠自分で自分がコメントしていて最終コメントが自分でないスレッド（自分のプルリクのコメントに自分が回答して、回答待ちのもの）
                    {
                        txtThread = "〇 " + txtThread;
                        lbThread.Items.Add(new DisplayData(txtThread, prUrl));
                    }
                    else
                    {
                        txtThread = "× " + txtThread;
                        if (cbAllDisp.IsChecked == true)
                            lbThread.Items.Add(new DisplayData(txtThread, prUrl));
                    }
                    Debug.WriteLine(txtThread);
                }


                ///////////////////////////////////
                // プルリク情報の表示
                ///////////////////////////////////
                var txtPr = $"{pr.title}, st：{pr.status}, 作成者：{pr.createdBy.displayName}, 作成日：{pr.creationDate.ToString("yyyy/MM/dd")}, {(DateTime.Now - pr.creationDate).Days}日経過, コメント数：{threadCount}";

                if (pr.createdBy.uniqueName == SelfMailAddr && pr.status == "active")
                {
                    txtPr = "〇" + txtPr;
                    lbPullRequest.Items.Add(new DisplayData(txtPr, prUrl));
                }
                else
                {
                    txtPr = "×" + txtPr;
                    if (cbAllDisp.IsChecked == true)
                        lbPullRequest.Items.Add(new DisplayData(txtPr, prUrl));
                }
                Debug.WriteLine(txtPr);
            }
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





public class RepositoryInfo
{
    public Value[] value { get; set; }
    public int count { get; set; }
}

public class Value
{
    public string id { get; set; }
    public string name { get; set; }
    public string url { get; set; }
    public Project project { get; set; }//Projectと名前があるが、実際はリポジトリ情報を格納している
    public string defaultBranch { get; set; }
    public int size { get; set; }
    public string remoteUrl { get; set; }
    public string sshUrl { get; set; }
    public string webUrl { get; set; }
    public bool isDisabled { get; set; }
    public bool isInMaintenance { get; set; }
}

public class Project
{
    public string id { get; set; }
    public string name { get; set; }
    public string url { get; set; }
    public string state { get; set; }
    public int revision { get; set; }
    public string visibility { get; set; }
    public DateTime lastUpdateTime { get; set; }

    public PullRequestsInfo PullRequests { get; set; }
}
