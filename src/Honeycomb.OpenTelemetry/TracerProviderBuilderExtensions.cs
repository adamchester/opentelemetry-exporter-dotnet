using System;
using Honeycomb.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;

namespace Honeycomb.OpenTelemetry
{
    public static class TracerProviderBuilderExtensions
    {
        public static TracerProviderBuilder UseHoneycomb(this TracerProviderBuilder builder, HoneycombExporter honeycombExporter)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            return builder.AddProcessor(new BatchExportActivityProcessor(honeycombExporter));
        }

        public static TracerProviderBuilder UseHoneycomb(this TracerProviderBuilder builder,
            IHoneycombService honeycombService,
            IOptions<HoneycombApiSettings> honeycombApiSettings)
        {
            return builder.UseHoneycomb(new HoneycombExporter(honeycombService, honeycombApiSettings));
        }

        public static TracerProviderBuilder UseHoneycomb(this TracerProviderBuilder builder, IServiceProvider serviceProvider)
        {
            return builder.UseHoneycomb(serviceProvider.GetRequiredService<HoneycombExporter>());
        }
    }
}
