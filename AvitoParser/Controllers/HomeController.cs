using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Web.Mvc;
using System.IO;

namespace AvitoParser.Controllers
{

    public class HomeController : Controller
    {
        const string MsgDeniedSite = "ERROR. Your site doesn't exist or temporary unavailable";
        const string MsgErrorGettingData = "ERROR. DATA HASN'T BEEN GOT FROM YOUR URL.";
        static Type ThisType = typeof(HomeController);


        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(string url)
        {
            List<Candidate> candidates = new List<Candidate>();
            string message = String.Empty;

            if (!String.IsNullOrEmpty(url))      
            {
                if (ResumeManager.IfUrlExists(url))
                {
                    Log.For(ThisType).Info(" (!!!) START processing. Url: " + url);
                    candidates = ResumeManager.GetCandidates(url);
                    if (candidates == null || candidates.Count == 0)
                        message = MsgErrorGettingData;
                    else
                    {
                        DBManager.CreateTableIfNotExists();
                        DBManager.SaveCandidatesToDB(candidates);
                        Log.For(ThisType).Info(" (!!!) FINISH processing. Url: " + url);
                        message = url;
                    }
                }
                else
                    message = MsgDeniedSite;

                return View("Info", (object)message);
            }
            else
                return RedirectToAction("Index");
        }



        [HttpPost]
        public JsonResult GetData()     
        {
            List<Candidate> candidates = DBManager.GetCandidatesFromDB();

            return Json(GetJsonObject(candidates), JsonRequestBehavior.AllowGet);
        }


        private Object GetJsonObject(List<Candidate> candidates)
        {
            object jsonData = new
            {
                rows = candidates.ToArray()
            };

            return jsonData;
        }




    }
}