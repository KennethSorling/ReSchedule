using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReScheduler.Models
{
    public class ScheduledItem
    {
        public string Name { get; set; }
        public DateTime Begins { get; set; }
        public int Duration { get; set; }
    }
}