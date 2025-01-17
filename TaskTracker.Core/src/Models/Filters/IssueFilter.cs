using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskTracker.Core.src.Models.Filters
{
    public class IssueFilter : BaseFilter
    {
        public int WorkspaceId { get; set; }

        public int ProjectId { get; set; }
    }
}
