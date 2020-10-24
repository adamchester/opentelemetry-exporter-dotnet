using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Honeycomb.OpenTelemetry;
using Honeycomb.Models;
using Honeycomb;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // Honeycomb Setup
            services.Configure<HoneycombApiSettings>(Configuration.GetSection("HoneycombSettings"));
            services.AddHttpClient("honeycomb");
            services.AddSingleton<IHoneycombService, HoneycombService>();
            services.AddSingleton<HoneycombExporter>();

            // OpenTelemetry Setup
            services.AddOpenTelemetryTracing((sp, builder) =>
                builder
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .SetResource(Resources.CreateServiceResource("sample"))
                    .UseHoneycomb(sp)
                    .AddConsoleExporter(c => c.DisplayAsJson = true));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
