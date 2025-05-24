using PullReqInfoCollector.Data;
using PullReqInfoCollector.Model;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PullReqInfoCollector;

public partial class MainWindow : Window
{
    AppService _app;
    RepositoryDataHandler _rdh;

    private record DisplayData(string DisplayString, string Url);

    public MainWindow()
    {
        InitializeComponent();

        _rdh = new RepositoryDataHandler();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // リポジトリ情報読み込み
        var setting = _rdh.Read();

        if (setting == null)
            return;

        (tbOrganizationName.Text, tbProjectName.Text, tbSelfMailAddr.Text) = setting.Value;

        NavigateToLogonView();

        // リポジトリに接続に行く（ログインしてなかったらPW入力画面、ログインしている場合はjsonが見える→json見えた場合は、ここを押してボタンを押してもらう）
        await Connect(new AppServiceInfo(tbOrganizationName.Text, tbProjectName.Text, tbSelfMailAddr.Text));
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _app.Dispose();
    }

    // プルリク情報読み込みボタン
    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        // 一旦表示をクリア
        //lbPullRequest.Items.Clear();
        lbThread.Items.Clear();
        var infos = await GetInformation();
        infos.ForEach(cm => lbThread.Items.Add(cm));
    }

    // ログイン画面を終わらせるボタン
    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        NavigateToPullRequestSearchView();
    }

    // 設定保存ボタン
    private void Button_Click_2(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(tbOrganizationName.Text) || string.IsNullOrEmpty(tbOrganizationName.Text) || string.IsNullOrEmpty(tbOrganizationName.Text))
        {
            MessageBox.Show("リポジトリ情報をちゃんと入れてください。");
            return;
        }

        // リポジトリ情報保存
        var filePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "RepoInfo.dat");
        File.AppendAllText(filePath, $"{tbOrganizationName.Text},{tbProjectName.Text},{tbSelfMailAddr.Text}");
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

    // ===================================================================================================

    private async Task Connect(AppServiceInfo asi)
    {
        if (_app == null)
            Disconnect();

        _app = new AppService();

        await _app.Initialize(webView2, asi);
    }

    private void Disconnect()
    {
        if (_app == null)
            return;

        _app.Dispose();
        _app = null;
    }

    private async Task<List<DisplayData>> GetInformation()
    {
        var infos = new List<DisplayData>();

        await _app.GetAllPullRequestCommentData((prInfo =>
        {
            // とりあえず条件なしで全部表示
            infos.AddRange(DispInfo(prInfo));
        }));

        return infos;
    }

    private List<DisplayData> DispInfo(PullRequestInfo pr)
    {
        var infos = new List<DisplayData>();
        var txtPr = $"{pr.Title}, st：{pr.Status}, 作成者：{pr.Author}, 作成日：{pr.CreationDate.ToString("yyyy/MM/dd")}, {(DateTime.Now - pr.CreationDate).Days}日経過, コメント数：{pr.threadInfos.Count()}";

        pr.threadInfos.ToList().ForEach(thread =>
        {
            var txtThread = $"{pr.RepositoryName}, {pr.Title} 先頭：{thread.Comments.First().ToString().Replace("\n", "")}, st：{thread.Status}, 先頭者：{thread.Comments.First().Author}, 末尾者：{thread.Comments.Last().Author}";
            infos.Add(new DisplayData(txtThread, pr.Url));
        });

        return infos;
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        var si = new ProcessStartInfo(e.Uri.AbsoluteUri);
        si.UseShellExecute = true;
        Process.Start(si);
        e.Handled = true;
    }
}
