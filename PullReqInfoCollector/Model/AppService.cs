using PullReqInfoCollector.Data.PullRequests;
using PullReqInfoCollector.Data.PullRequestsThreads;
using PullReqInfoCollector.Data.Repository;
using PullReqInfoCollector.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Microsoft.Web.WebView2.Wpf;

namespace PullReqInfoCollector.Model
{
    internal class AppService
    {
        WebAccess? _wa;
        AppServiceInfo? _appServiceInfo;

        internal async Task Initialize(WebView2 webView2, AppServiceInfo appServiceInfo)
        {
            _appServiceInfo = appServiceInfo;
            _wa = new WebAccess(webView2);
            await _wa.Initialize($"https://dev.azure.com/{appServiceInfo.OrganizatioinName}/{appServiceInfo.ProjectName}/_apis/git/repositories?api-version=7.1-preview.1&searchCriteria.status=all");
        }

        internal async Task GetAllPullRequestCommentData(Action<PullRequestInfo> PullRequestFound)
        {
            var repoInfo = await _wa.GetContentAsync<JsonRepositoryInfo>($"https://dev.azure.com/{_appServiceInfo.OrganizatioinName}/{_appServiceInfo.ProjectName}/_apis/git/repositories?api-version=7.1-preview.1&searchCriteria.status=all");

            if (repoInfo is null)
            {
                Debug.WriteLine("リポジトリ情報がありませんでした。");
                return;
            }

            var repoCtr = repoInfo.count;

            // projectがリポジトリ名っぽい

            JsonPullRequestsInfo? prInfo;

            for (int k = 0; k < repoCtr; k++)//k:リポジトリ番号
            {
                var repo = repoInfo.value[k];

                // リポジトリ毎の全プルリク情報
                prInfo = await _wa.GetContentAsync<JsonPullRequestsInfo>($"https://dev.azure.com/{_appServiceInfo.OrganizatioinName}/{_appServiceInfo.ProjectName}/_apis/git/repositories/{repo.name}/pullrequests?api-version=7.1-preview.1&searchCriteria.status=all");

                repo.project.PullRequests = prInfo;

                var ctr = prInfo.value.Count();

                for (int l = 0; l < ctr; l++)//l:プルリク番号
                {
                    var prUrl = "";
                    var pr = repo.project.PullRequests.value[l];

                    // プルリク毎の全スレッド情報
                    var thInfo = await _wa.GetContentAsync<JsonPullRequestsThreadsInfo>($"https://dev.azure.com/{_appServiceInfo.OrganizatioinName}/{_appServiceInfo.ProjectName}/_apis/git/repositories/{repo.name}/pullRequests/{pr.pullRequestId}/threads?api-version=7.1-preview.1");

                    pr.threadsInfo = thInfo;
                    var threadCount = pr.threadsInfo.value.Count();

                    var threadList = new List<ThreadInfo>();

                    for (int j = 0; j < threadCount; j++)
                    {
                        var thread = pr.threadsInfo.value[j];

                        if (thread.comments is null || thread.comments.Length == 0)
                            continue;

                        var href = thread.comments.First()._links.self.href;
                        prUrl = GetPullReqUrlFromPullReqRefString(_appServiceInfo.OrganizatioinName, _appServiceInfo.ProjectName, repo.name, href);
                        //var prId = GetPullReqIdFromPullReqRefString(href);


                        if (thread.comments.First().commentType == "system")
                            continue;

                        ///////////////////////////////////
                        // スレッド(コメント情報の表示)
                        ///////////////////////////////////
                        var txtThread = $"{repo.name}, {pr.title} 先頭：{thread.comments.First().content.ToString().Replace("\n", "")}, st：{thread.status}, 先頭者：{thread.comments.First().author.uniqueName}, 末尾者：{thread.comments.Last().author.uniqueName}";
                        //var txtThread = $"{thread.comments.First().content.ToString().Replace("\n", "")}";


                        var st = thread.status == "active" ? ThreadStatus.Active : ThreadStatus.NotActive;
                        var comments = thread.comments.Select(x => (x.author.uniqueName, x.content.ToString().Replace("\n", ""))).ToImmutableList();

                        threadList.Add(new ThreadInfo(st, thread.comments.First().author.uniqueName, comments));

                        //if ((thread.status == "active" && thread.comments.First().author.uniqueName == appServiceInfo.SelfMailAddr && thread.comments.Last().author.uniqueName != appServiceInfo.SelfMailAddr)
                        //    // スレッド作成者＝自分でActiveなスレッドで最終コメントが自分でないスレッド（人のプルリクにコメントして、回答が来てるもの）
                        //    || (thread.status == "active" && thread.comments.First().author.uniqueName != appServiceInfo.SelfMailAddr && thread.comments.Last().author.uniqueName != appServiceInfo.SelfMailAddr && thread.comments.Any(x => x.author.uniqueName == appServiceInfo.SelfMailAddr)))
                        //// スレッド作成者≠自分で自分がコメントしていて最終コメントが自分でないスレッド（自分のプルリクのコメントに自分が回答して、回答待ちのもの）
                        //{
                        //    //txtThread = "〇 " + txtThread;
                        //    lbThread.Items.Add(new DisplayData(txtThread, prUrl));
                        //}
                        //else
                        //{
                        //    //txtThread = "× " + txtThread;
                        //    if (cbAllDisp.IsChecked == true)
                        //        lbThread.Items.Add(new DisplayData(txtThread, prUrl));
                        //}

                        Debug.WriteLine(txtThread);
                    }

                    ///////////////////////////////////
                    // プルリク情報の表示
                    ///////////////////////////////////
                    var txtPr = $"{pr.title}, st：{pr.status}, 作成者：{pr.createdBy.displayName}, 作成日：{pr.creationDate.ToString("yyyy/MM/dd")}, {(DateTime.Now - pr.creationDate).Days}日経過, コメント数：{threadCount}";

                    var st2 = pr.status == "active" ? PullRequestStatus.Active : PullRequestStatus.NotActive;
                    PullRequestFound.Invoke(new PullRequestInfo(repo.name, pr.title, st2, pr.createdBy.displayName, pr.creationDate, prUrl, threadList));

                    //if (pr.createdBy.uniqueName == appServiceInfo.SelfMailAddr && pr.status == "active")
                    //{
                    //    txtPr = "〇" + txtPr;
                    //    lbPullRequest.Items.Add(new DisplayData(txtPr, prUrl));
                    //}
                    //else
                    //{
                    //    txtPr = "×" + txtPr;
                    //    if (cbAllDisp.IsChecked == true)
                    //        lbPullRequest.Items.Add(new DisplayData(txtPr, prUrl));
                    //}

                    //Debug.WriteLine(txtPr);
                }
            }
        }

        private string GetPullReqUrlFromPullReqRefString(string OrganizatioinName, string ProjectName, string repoName, string prReq)
        {
            var parts = prReq.Split('/');

            // https://dev.azure.com/tera1707/_apis/git/repositories/ec19976b-a797-4184-9c1b-22d5e9d2e837/pullRequests/3/threads/4/comments/1
            // というリンクの後ろから5つ目がプルリクID、一番最後がスレッドID
            var prId = int.Parse(parts[parts.Count() - 5]);

            var prUrl = $"https://dev.azure.com/{OrganizatioinName}/{ProjectName}/_git/{repoName}/pullRequest/{prId}";///thread/{threadId}";

            return prUrl;
        }
    }
}
