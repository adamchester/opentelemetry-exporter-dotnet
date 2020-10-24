using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Honeycomb.Models;
using OpenTelemetry;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using OpenTelemetry.Trace;

namespace Honeycomb.OpenTelemetry
{
    public class HoneycombExporter : ActivityExporter
    {
        private readonly IHoneycombService _honeycombService;
        private readonly IOptions<HoneycombApiSettings> _settings;

        public HoneycombExporter(IHoneycombService honeycombService, IOptions<HoneycombApiSettings> settings)
        {
            _honeycombService = honeycombService;
            _settings = settings;
        }

        public override ExportResult Export(in Batch<Activity> batch)
        {
            using var scope = SuppressInstrumentationScope.Begin();
            var honeycombEvents = new List<HoneycombEvent>();
            foreach (var activity in batch)
            {
                honeycombEvents.AddRange(GenerateEvent(activity));
            }

            try
            {
                _honeycombService.SendBatchAsync(honeycombEvents).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                HoneycombExporterEventSource.Log.FailedExport(ex);
                return ExportResult.Failure;
            }

            return ExportResult.Success;
        }

        private IEnumerable<HoneycombEvent> GenerateEvent(Activity activity)
        {
            var list = new List<HoneycombEvent>();

            var ev = new HoneycombEvent
            {
                EventTime = activity.StartTimeUtc,
                DataSetName = _settings.Value.DefaultDataSet,
            };

            var resource = activity.GetResource();
            var serviceName = resource
                                  ?.Attributes
                                  ?.FirstOrDefault(
                                      a => a.Key == global::OpenTelemetry.Resources.Resource.ServiceNameKey)
                                  .Value
                              ?? "";

            var baseAttributes = new Dictionary<string, object>
            {
                {"trace.trace_id", activity.Context.TraceId.ToString()},
            };

            if (activity.ParentSpanId.ToString() != "0000000000000000")
                ev.Data.Add("trace.parent_id", activity.ParentSpanId.ToString());

            ev.Data.AddRange(baseAttributes);
            ev.Data.Add("trace.span_id", activity.Context.SpanId.ToString());
            ev.Data.Add("duration_ms", activity.Duration.TotalMilliseconds);

            foreach (var label in activity.Baggage)
            {
                ev.Data.Add(label.Key, label.Value);
            }

            foreach (var attr in activity.GetResource().Attributes)
            {
                ev.Data.Add(attr.Key, attr.Value);
            }

            foreach (var message in activity.Events)
            {
                var messageEvent = new HoneycombEvent
                {
                    EventTime = message.Timestamp.UtcDateTime,
                    DataSetName = _settings.Value.DefaultDataSet,
                    Data = message.Tags.ToDictionary(a => a.Key, a => a.Value)
                };
                messageEvent.Data.Add("meta.annotation_type", "span_event");
                messageEvent.Data.Add("trace.parent_id", activity.Context.SpanId.ToString());
                messageEvent.Data.Add("name", message.Name);
                messageEvent.Data.AddRange(baseAttributes);
                list.Add(messageEvent);
            }

            foreach (var link in activity.Links)
            {
                var linkEvent = new HoneycombEvent
                {
                    EventTime = activity.StartTimeUtc,
                    DataSetName = _settings.Value.DefaultDataSet,
                    Data = link.Tags?
                               .ToDictionary(a => a.Key, a => a.Value)
                           ?? new Dictionary<string, object>()
                };
                linkEvent.Data.Add("meta.annotation_type", "link");
                linkEvent.Data.Add("trace.link.span_id", link.Context.SpanId.ToString());
                linkEvent.Data.Add("trace.link.trace_id", link.Context.TraceId.ToString());
                linkEvent.Data.AddRange(baseAttributes);
                list.Add(linkEvent);
            }

            list.Add(ev);
            return list;
        }

    }

    public static class DictionaryExtensions
    {
        public static void AddRange<T, T1>(this Dictionary<T, T1> dest, Dictionary<T, T1> source)
        {
            foreach (var kvp in source)
                dest.Add(kvp.Key, kvp.Value);
        }
    }
}
