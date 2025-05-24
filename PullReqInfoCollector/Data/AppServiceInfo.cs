using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PullReqInfoCollector.Data
{
    public record AppServiceInfo(string OrganizatioinName, string ProjectName, string SelfMailAddr);
    //{
    //    public string OrganizatioinName = "tera1707";
    //    public string ProjectName = "TeraPrivateProject";
    //    //private string RepositoryName = "TeraPrivateProject";
    //    public string SelfMailAddr = "tera1707@gmail.com";
    //}
}
