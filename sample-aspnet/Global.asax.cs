using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Honeycomb;
using Honeycomb.Models;
using Honeycomb.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace sample_aspnet
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private TracerProvider _tracerProvider;

        protected void Application_Start()
        {
            Serilog.Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Seq("http://localhost:5341")
                .CreateLogger();

            var services = new ServiceCollection();
            services.Configure<HoneycombApiSettings>(c =>
            {
                c.BatchSize = 100;
                c.SendFrequency = 10000;
                c.DefaultDataSet = ConfigurationManager.AppSettings["HoneycombDefaultDataSet"];
                c.TeamId = ConfigurationManager.AppSettings["HoneycombTeamId"];
            });
            services.AddHttpClient("honeycomb");
            services.AddSingleton<IHoneycombService, HoneycombService>();
            services.AddSingleton<HoneycombExporter>();
            var serviceProvider = services.BuildServiceProvider();

            _tracerProvider = Sdk.CreateTracerProviderBuilder()
                .AddAspNetInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
                .SetResource(Resources.CreateServiceResource("my-service-name"))
                .UseHoneycomb(serviceProvider)
                .Build();

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        public override void Dispose()
        {
            _tracerProvider?.Dispose();
        }
    }
}
