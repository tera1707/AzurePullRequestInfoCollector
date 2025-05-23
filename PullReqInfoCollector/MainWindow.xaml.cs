using PullReqInfoCollector.Data;
using PullReqInfoCollector.Model;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace PullReqInfoCollector;

public partial class MainWindow : Window
{
    private string OrganizatioinName = "tera1707";
    private string ProjectName = "TeraPrivateProject";
    //private string RepositoryName = "TeraPrivateProject";
    private string SelfMailAddr = "tera1707@gmail.com";

    AppService app;

    private record DisplayData(string DisplayString, string Url);

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        //wa = new WebAccess(webView2);
        //await wa.Initialize($"https://dev.azure.com/{OrganizatioinName}/{ProjectName}/_apis/git/repositories?api-version=7.1-preview.1&searchCriteria.status=all");

        app = new AppService();

        await app.Initialize(webView2, new AppServiceInfo());

        btGetInfo.IsEnabled = false;
        await Task.Delay(1000);
        btGetInfo.IsEnabled = true;
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        await GetInformation();
    }


    private async Task GetInformation()
    {
        // 一旦表示をクリア
        lbPullRequest.Items.Clear();
        lbThread.Items.Clear();


        await app.GetAllPullRequestCommentData((prInfo =>
        {
            void DispInfo(PullRequestInfo pr)
            {
                var txtPr = $"{pr.Title}, st：{pr.Status}, 作成者：{pr.Author}, 作成日：{pr.CreationDate.ToString("yyyy/MM/dd")}, {(DateTime.Now - pr.CreationDate).Days}日経過, コメント数：{pr.threadInfos.Count()}";
                lbPullRequest.Items.Add(new DisplayData(txtPr, prInfo.Url));

                pr.threadInfos.ToList().ForEach(thread =>
                {
                    var txtThread = $"{pr.RepositoryName}, {pr.Title} 先頭：{thread.Comments.First().ToString().Replace("\n", "")}, st：{thread.Status}, 先頭者：{thread.Comments.First().Author}, 末尾者：{thread.Comments.Last().Author}";
                    lbThread.Items.Add(new DisplayData(txtThread, prInfo.Url));
                });
            }

            // とりあえず条件なしで全部表示
            DispInfo(prInfo);
        }));
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        var si = new ProcessStartInfo(e.Uri.AbsoluteUri);
        si.UseShellExecute = true;
        Process.Start(si);
        e.Handled = true;
    }
}




