using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;

using ReScheduler.Models;
using ReScheduler.ViewModels;
using Newtonsoft.Json;

namespace ReScheduler
{
    public class Consolidator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId">Numeric id of a user whose calendars we manage. Currently ignored</param>
        /// <param name="fromDate">Start date time of timespan to view, such as a week. Currently ignored.</param>
        /// <param name="toDate">End date and time of timespan to view, such as a week. Currently ignored.</param>
        /// <returns>A consolidated list of PassVM entries, with some events possiblu culled.</returns>
        public List<PassVM> GetConsolidatedSchedule(int userId, DateTime fromDate, DateTime toDate)
        {
            List<Pass> workRelated = GetWorkSchedule(userId, fromDate, toDate);
            List<Pass> workouts = GetExerciseSchedule(userId, fromDate, toDate);
            List<Pass> realLife = GetPrivateLifeSchedule(userId, fromDate, toDate);

            var collected = new List<Pass>();
            workRelated.ForEach(p => collected.Add(p));
            workouts.ForEach(p => collected.Add(p));
            realLife.ForEach(p => collected.Add(p));

            List<Pass> sorted = collected.OrderBy(p => p.StartTime).ThenByDescending(p => p.Importance).ToList();
            sorted = ResolveConflicts(sorted);
            return sorted.ConvertAll(c => new PassVM
            {
                Name = c.Name,
                StartTime = c.StartTime,
                EndTime = c.EndTime,
                Id = c.Id
            }
            );
        }
        
        /// <summary>
        /// Identifies scheduling conflicts and removes entries which can't fit into the schedule.
        /// </summary>
        /// <param name="conflicted">Original schedule, a List of Pass objects.</param>
        /// <returns></returns>
        private List<Pass> ResolveConflicts(List<Pass> conflicted)
        {
            //one item or fewer? No possible conflict.
            if (conflicted.Count <= 1) return conflicted;

            // starting with the second, compare every item with the preceding one.
            for (int i = 1; i < conflicted.Count; i++)
            {
                Pass current = conflicted[i];
                Pass previous = conflicted[i - 1];
                if (Clashes(previous, current))
                {
                    // if the previous item is of equal or greater importance
                    if (previous.Importance >= current.Importance)
                    // .. we mark the current one for culling.
                    { current.Importance = -1; }
                    else { previous.Importance = -1; } // otherwise,  mark previous for culling.
                }
            }
            // we cull the the losers.
            return conflicted.Where(p => p.Importance >= 0).ToList();
        }

        /// <summary>
        /// Determines if two calendar events clash timewise.
        /// </summary>
        /// <param name="p1">First event. A Pass object. Begins first, or possibly at the same time.</param>
        /// <param name="p2">Second event. A Pass object. Begins after, or possibly at the same time as p1.</param>
        /// <returns></returns>
        private bool Clashes(Pass p1, Pass p2)
        {
            //the passes come pre-sorted.
            //p1 will occur before p2 or at the same time.
            if (p1.StartTime == p2.StartTime) return true;
            if (p1.EndTime > p2.StartTime) return true;
            return false;
        }

        /// <summary>
        /// Gets a list of events from the user's private schedule.  They are converted to a common format in the process.
        /// </summary>
        /// <param name="userId">Id of user whose schedule we get</param>
        /// <param name="fromDate">Start of the timespan to view.</param>
        /// <param name="toDate">End of the timespan to view.</param>
        /// <returns>a List of Pass objects.</returns>
        public List<Pass> GetPrivateLifeSchedule(int userId, DateTime fromDate, DateTime toDate)
        {
            var prefs = GetUserPriorities(userId);
            int importance = prefs[ActivityType.Social];
            string textData = FetchJson("socialcalendar.json");
            List<ScheduledItem> socialCalendar = JsonConvert.DeserializeObject<List<ScheduledItem>>(textData);

            List<Pass> converted = socialCalendar.ConvertAll
                (s => new Pass { Name = s.Name, StartTime = s.Begins, EndTime = s.Begins.AddMinutes(s.Duration), Importance = importance });
            return converted;
        }

        /// <summary>
        /// Gets a list of events from a user's exercise schedule. They are converted to a common format in the process.
        /// </summary>
        /// <param name="userId">Id of user whose schedule we get</param>
        /// <param name="fromDate">Start of the timespan to view.</param>
        /// <param name="toDate">End of the timespan to view.</param>
        /// <returns>a List of Pass objects.</returns>
        public List<Pass> GetExerciseSchedule(int userId, DateTime fromDate, DateTime toDate)
        {
            var prefs = GetUserPriorities(userId);
            int importance = prefs[ActivityType.Exercise];
            string textData = FetchJson("exercisecalendar.json");
            List<ExerciseItem> exerciseCalendar = JsonConvert.DeserializeObject<List<ExerciseItem>>(textData);

            //We adjust the start and end times to accommodate the minutes needed before and after the activity.
            List<Pass> converted = exerciseCalendar.ConvertAll
                (e => new Pass
                {
                    Name = e.Name,
                    StartTime = e.StartTime.AddMinutes(-(e.PrepTimeMinutes)),
                    EndTime = e.EndTime.AddMinutes(e.WrapUpTimeMinutes),
                    Importance = importance,
                    UserId = userId
                    }
                );
            return converted;
        }

        /// <summary>
        /// Obtains a list of eventsfrom the user's work schedule.  They are converted to a common format in the process.
        /// </summary>
        /// <param name="userId">Id of user whose schedule we get</param>
        /// <param name="fromDate">Start of the timespan to view.</param>
        /// <param name="toDate">End of the timespan to view.</param>
        /// <returns>a List of Pass objects.</returns>
        public List<Pass> GetWorkSchedule(int userId, DateTime fromDate, DateTime toDate)
        {
            var prefs = GetUserPriorities(userId);
            int importance = prefs[ActivityType.WorkRelated];
            string textData = FetchJson("workcalendar.json");
            List<WorkItem> workCalendar = JsonConvert.DeserializeObject<List<WorkItem>>(textData);
            List<Pass> converted = workCalendar.ConvertAll
                (w => new Pass
                {
                    Name = w.Name,
                    StartTime = w.Starts,
                    EndTime = w.Ends,
                    UserId = userId,
                    Importance = importance
                }
                );
            return converted;
        }

        /// <summary>
        /// Simulates obtaining the user's priorities vis-a-vis the types of activity
        /// </summary>
        /// <param name="userId">Id of the user</param>
        /// <returns>A dictionary keyed on activity type, which assigns weight to various activity types.</returns>
        private Dictionary<ActivityType, int> GetUserPriorities(int userId)
        {
            var priorities = new Dictionary<ActivityType, int>();
            priorities.Add(ActivityType.WorkRelated, 500);
            priorities.Add(ActivityType.Social, 400);
            priorities.Add(ActivityType.Exercise, 300);
            //currently, these prefs below will be treated as having the weight 300.
            priorities.Add(ActivityType.Boxing, 320);
            priorities.Add(ActivityType.Spinning, 310);
            priorities.Add(ActivityType.Yoga, 305);
            return priorities;
        }

        /// <summary>
        /// Generates sample json data files in the "My Documents" folder. Necessary for verification and experimentation.
        /// </summary>
        public void CreateSampleData()
        {
            var jobs = new List<WorkItem>();
            jobs.Add(new WorkItem { Name = "Lunch", Starts = DateTime.Parse("2017-06-26 12:00:00"), Ends = DateTime.Parse("2017-06-26 13:00:00") });
            jobs.Add(new WorkItem { Name = "Lunch", Starts = DateTime.Parse("2017-06-27 12:00:00"), Ends = DateTime.Parse("2017-06-27 13:00:00") });
            jobs.Add(new WorkItem { Name = "Lunch", Starts = DateTime.Parse("2017-06-28 12:00:00"), Ends = DateTime.Parse("2017-06-28 13:00:00") });
            jobs.Add(new WorkItem { Name = "Lunch", Starts = DateTime.Parse("2017-06-29 12:00:00"), Ends = DateTime.Parse("2017-06-29 13:00:00") });
            jobs.Add(new WorkItem { Name = "Lunch", Starts = DateTime.Parse("2017-06-30 12:00:00"), Ends = DateTime.Parse("2017-06-30 13:00:00") });
            jobs.Add(new WorkItem { Name = "Afterwork", Starts = DateTime.Parse("2017-06-30 17:00:00"), Ends = DateTime.Parse("2017-06-30 19:00:00") });

            SaveJson<List<WorkItem>>(jobs, "workcalendar.json");

            var workouts = new List<ExerciseItem>();
            //this one will clash with a hot date to be added later, so will beculled
            workouts.Add(new ExerciseItem
            {
                Name = "Yoga",
                StartTime = DateTime.Parse("2017-06-27 18:00:00"),
                EndTime = DateTime.Parse("2017-06-27 19:00:00"),
                PrepTimeMinutes = 25,
                WrapUpTimeMinutes = 25
            });
            //this pass and the next will clash timewise.
            //currently, the first one will be kept
            workouts.Add(new ExerciseItem
            {
                Name = "Spinning",
                StartTime = DateTime.Parse("2017-06-28 18:00:00"),
                EndTime = DateTime.Parse("2017-06-28 19:00:00"),
                PrepTimeMinutes = 20,
                WrapUpTimeMinutes = 20
            });

            workouts.Add(new ExerciseItem
            {
                Name = "Boxing",
                StartTime = DateTime.Parse("2017-06-28 18:00:00"),
                EndTime = DateTime.Parse("2017-06-28 19:00:00"),
                PrepTimeMinutes = 30,
                WrapUpTimeMinutes = 30
            });
            workouts.Add(new ExerciseItem
            {
                Name = "Spinning",
                StartTime = DateTime.Parse("2017-06-29 18:00:00"),
                EndTime = DateTime.Parse("2017-06-29 19:00:00"),
                PrepTimeMinutes = 20,
                WrapUpTimeMinutes = 20
            });
            //this item will clash with social activity "Kyrkan" in the socal schedule.
            workouts.Add(new ExerciseItem
            {
                Name = "Jogging",
                StartTime = DateTime.Parse("2017-07-02 11:00:00"),
                EndTime = DateTime.Parse("2017-07-02 11:45:00"),
                PrepTimeMinutes = 0,
                WrapUpTimeMinutes = 0
            });

            SaveJson<List<ExerciseItem>>(workouts, "exercisecalendar.json");

            var funTimes = new List<ScheduledItem>();
            funTimes.Add(new ScheduledItem
            {
                Name = "Dejt",
                Begins = DateTime.Parse("2017-06-27 18:00:00"),
                Duration = 120
            });
            funTimes.Add(new ScheduledItem
            {
                Name = "Kyrkan",
                Begins = DateTime.Parse("2017-07-02 11:00:00"),
                Duration = 90
            });
            SaveJson<List<ScheduledItem>>(funTimes, "socialcalendar.json");


        }

        /// <summary>
        /// Helper generic function to save an object of whatever type to a JSON file.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="schedule">Typically, a list of Pass, ExerciseItem, WorkItem, or ScheduledItem object</param>
        /// <param name="fileName">Simple filename, without path, to save to. The path to your Documents folder will be prepended.</param>
        public void SaveJson<T>(T schedule, string fileName)
        {
            string textData = JsonConvert.SerializeObject(schedule);
            string fullPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                fileName);
            System.IO.File.WriteAllText(fullPath, textData);
        }


        public string FetchJson(string fileName)
        {
            string fullPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    fileName);

            return System.IO.File.ReadAllText(fullPath);
        }
    }
}