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
        NavigateToLogonView();
        await Connect(new AppServiceInfo(tbOrganizationName.Text, tbProjectName.Text, tbSelfMailAddr.Text));
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        app.Dispose();
    }

    // プルリク情報読み込みボタン
    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        await GetInformation();
    }

    // ログイン画面を終わらせるボタン
    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        NavigateToPullRequestSearchView();
    }

    // 設定保存ボタン
    private void Button_Click_2(object sender, RoutedEventArgs e)
    {

    }

    private void NavigateToLogonView()
    {
        WebViewArea.Visibility = Visibility.Visible;
        PullReqCommentSearchArea.Visibility = Visibility.Collapsed;
    }

    private void NavigateToPullRequestSearchView()
    {
        WebViewArea.Visibility = Visibility.Collapsed;
        PullReqCommentSearchArea.Visibility = Visibility.Visible;
        btGetInfo.IsEnabled = true;
    }

    private async Task Connect(AppServiceInfo asi)
    {
        if (app == null)
            Disconnect();

        app = new AppService();

        await app.Initialize(webView2, asi);

        // とりあえず1秒待つ
        await Task.Delay(1000);
    }

    private void Disconnect()
    {
        if (app == null)
            return;

        app.Dispose();
        app = null;
    }

    private async Task GetInformation()
    {
        // 一旦表示をクリア
        //lbPullRequest.Items.Clear();
        lbThread.Items.Clear();


        await app.GetAllPullRequestCommentData((prInfo =>
        {
            // とりあえず条件なしで全部表示
            DispInfo(prInfo);
        }));
    }

    void DispInfo(PullRequestInfo pr)
    {
        var txtPr = $"{pr.Title}, st：{pr.Status}, 作成者：{pr.Author}, 作成日：{pr.CreationDate.ToString("yyyy/MM/dd")}, {(DateTime.Now - pr.CreationDate).Days}日経過, コメント数：{pr.threadInfos.Count()}";
        //lbPullRequest.Items.Add(new DisplayData(txtPr, pr.Url));

        pr.threadInfos.ToList().ForEach(thread =>
        {
            var txtThread = $"{pr.RepositoryName}, {pr.Title} 先頭：{thread.Comments.First().ToString().Replace("\n", "")}, st：{thread.Status}, 先頭者：{thread.Comments.First().Author}, 末尾者：{thread.Comments.Last().Author}";
            lbThread.Items.Add(new DisplayData(txtThread, pr.Url));
        });
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        var si = new ProcessStartInfo(e.Uri.AbsoluteUri);
        si.UseShellExecute = true;
        Process.Start(si);
        e.Handled = true;
    }
}




