using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ReScheduler.ViewModels
{
    public class PassVM
    {
        public int Id { get; set; }                 // Your DB Id for the atctivity
        public int UserId { get; set; }             // DB Id of the user of this particular schedule
        public string Name { get; set; }            // Short name for the activity
        public string Details { get; set; }         //  More (optional) info about  it.
        public DateTime StartTime { get; set; }     //  When it begins
        public DateTime EndTime { get; set; }       //  When it ends
        public int ReminderTimeMinutes { get; set;} // Time, in minutes, in advance, to alert the user
        public bool WantReminder { get; set; }      // Whether to alert the user ahead of time.
    }

}