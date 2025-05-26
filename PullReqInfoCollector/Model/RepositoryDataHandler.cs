using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PullReqInfoCollector.Model
{
    internal class RepositoryDataHandler
    {
        internal (string tbOrganizationName, string ProjectName)? Read()
        {

            // リポジトリ情報読み込み
            var filePath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "RepoInfo.dat");

            if (!System.IO.Path.Exists(filePath))
                return null;

            var settings = File.ReadAllText(filePath).Split(',');


            return (settings[0], settings[1]);
        }

        internal void Write(string OrganizationName, string ProjectName)
        {
            // リポジトリ情報保存
            var filePath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "RepoInfo.dat");
            File.WriteAllText(filePath, $"{OrganizationName},{ProjectName}");
        }
    }
}
