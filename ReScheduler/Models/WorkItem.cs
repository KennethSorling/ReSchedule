using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReScheduler.Models
{
    public class WorkItem
    {
        public string Name { get; set; }
        public DateTime Starts { get; set; }
        public DateTime Ends { get; set; }
    }
}