using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PullReqInfoCollector.Data;

public enum ThreadStatus
{
    Active,
    NotActive,
}

public record ThreadInfo(ThreadStatus Status, string Author, IReadOnlyList<(string Author, string Comment)> Comments);
