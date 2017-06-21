using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReScheduler.Models
{
    public class ExerciseItem
    {
        public string Name { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int PrepTimeMinutes { get; set; }           // Preparation time, in minutes, needed before the activity
        public int WrapUpTimeMinutes { get; set; }         // Time margin needed, in minutes, after the activity
    }
}