using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using static PullReqInfoCollector.MainWindow;

namespace PullReqInfoCollector.Data;

public enum PullRequestStatus
{
    Active,
    NotActive,
}

// タイトル, 作成者, 作成日時, コメント数
public record PullRequestInfo(string RepositoryName, string Title, PullRequestStatus Status, string Author, DateTime CreationDate, string Url, IReadOnlyList<ThreadInfo> threadInfos);
