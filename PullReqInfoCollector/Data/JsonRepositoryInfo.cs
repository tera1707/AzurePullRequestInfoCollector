using PullReqInfoCollector.Data.PullRequests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PullReqInfoCollector.Data.Repository;
public class JsonRepositoryInfo
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

    public JsonPullRequestsInfo PullRequests { get; set; }
}

