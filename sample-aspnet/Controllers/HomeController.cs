using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Serilog;
using Serilog.Context;
using SerilogTimings;

namespace sample_aspnet.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync("http://httpstat.us/200").GetAwaiter().GetResult();
                var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var tags = new ActivityTagsCollection(new Dictionary<string, object>
                {
                    {"response.body", responseBody},
                    {"response.status_code", (int) response.StatusCode},
                    {"response.status", response.StatusCode},
                });
                Activity.Current?.AddEvent(new ActivityEvent("http status 200 response", tags: tags));
            }

            using (LogContext.PushProperty("AnyOldValue", Guid.NewGuid()))
            using (var op = Operation.Begin("queuing it"))
            {
                System.Web.Hosting.HostingEnvironment.QueueBackgroundWorkItem(ct =>
                {
                    Log.Information("Hey we're inside!");
                });
                op.Complete("result", "queued");
            }

            using (LogContext.PushProperty("AnyOldValue", Guid.NewGuid()))
            using (var op = Operation.Begin("queuing it"))
            {
                var queued = ThreadPool.QueueUserWorkItem(state =>
                {
                    Log.Information("Hey we're inside!");
                });
                op.Complete("result", "queued");
            }

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
