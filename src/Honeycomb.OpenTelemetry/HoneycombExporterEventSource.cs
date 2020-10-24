using System;
using System.Diagnostics.Tracing;
using OpenTelemetry.Internal;

namespace Honeycomb.OpenTelemetry
{
    /// <summary>
    /// EventSource events emitted from the project.
    /// </summary>
    [EventSource(Name = "OpenTelemetry-Exporter-Honeycomb")]
    internal class HoneycombExporterEventSource : EventSource
    {
        public static HoneycombExporterEventSource Log = new HoneycombExporterEventSource();

        [NonEvent]
        public void FailedExport(Exception ex)
        {
            if (this.IsEnabled(EventLevel.Error, (EventKeywords)(-1)))
            {
                this.FailedExport(ex.ToInvariantString());
            }
        }

        [Event(1, Message = "Failed to export activities: '{0}'", Level = EventLevel.Error)]
        public void FailedExport(string exception)
        {
            this.WriteEvent(1, exception);
        }
    }
}
