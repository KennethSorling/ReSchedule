using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ReScheduler.Models;
using ReScheduler.ViewModels;
using Newtonsoft.Json;
using System.IO;

namespace ReScheduler.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {

            return View();
        }

        public ActionResult ConsolidatedSchedule()
        {
            int userId = 1004;
            DateTime startTime = DateTime.Parse("2017-06-26 00:00:00");
            DateTime endTime = DateTime.Parse("2017-07-02 23:59:59");

            var consolidator = new Consolidator();
            List<PassVM> consolidatedSchedule = consolidator.GetConsolidatedSchedule(userId, startTime, endTime);
            return View(consolidatedSchedule);
        }

        public ActionResult SocialSchedule()
        {
            int userId = 1004;
            DateTime startTime = DateTime.Parse("2017-06-26 00:00:00");
            DateTime endTime = DateTime.Parse("2017-07-02 23:59:59");
            var consolidator = new Consolidator();
            List<Pass> socialSchedule = consolidator.GetPrivateLifeSchedule(userId, startTime, endTime);
            return View(socialSchedule);
        }

        public ActionResult ExerciseSchedule()
        {
            int userId = 1004;
            DateTime startTime = DateTime.Parse("2017-06-26 00:00:00");
            DateTime endTime = DateTime.Parse("2017-07-02 23:59:59");
            var consolidator = new Consolidator();
            List<Pass> exerciseSchedule = consolidator.GetExerciseSchedule(userId, startTime, endTime);
            return View(exerciseSchedule);

        }
        public ActionResult WorkSchedule()
        {
            int userId = 1004;
            DateTime startTime = DateTime.Parse("2017-06-26 00:00:00");
            DateTime endTime = DateTime.Parse("2017-07-02 23:59:59");
            var consolidator = new Consolidator();
            List<Pass> workSchedule = consolidator.GetWorkSchedule(userId, startTime, endTime);
            return View(workSchedule);
        }

        public ActionResult CreateSamples()
        {
            var consolidator = new Consolidator();
            consolidator.CreateSampleData();
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }
        
        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

    }
}