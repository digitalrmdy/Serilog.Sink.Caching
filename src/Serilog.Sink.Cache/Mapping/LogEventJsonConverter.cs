using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Events;

namespace Serilog.Sink.Cache.Mapping
{
    internal class LogEventJsonConverter : JsonConverter<LogEvent>
    {
        private readonly LogEventPropertyConverter _logEventPropertyConverter;

        public LogEventJsonConverter()
        {
            _logEventPropertyConverter = new LogEventPropertyConverter();
        }

        public override void WriteJson(JsonWriter writer, LogEvent value, JsonSerializer serializer)
        {
            var dict = new Dictionary<string, object>
            {
                {nameof(LogEvent.Timestamp), value.Timestamp},
                {nameof(LogEvent.Level), value.Level.ToString()},
                {nameof(LogEvent.Exception), value.Exception},
                {nameof(LogEvent.MessageTemplate), value.MessageTemplate},
                {nameof(LogEvent.Properties), value.Properties?.Select(kvp => _logEventPropertyConverter.SerializeLogEventProperty(new LogEventProperty(kvp.Key, kvp.Value)))?.ToArray()}
            };

            JObject.FromObject(dict).WriteTo(writer);
        }

        public override LogEvent ReadJson(JsonReader reader, Type objectType, LogEvent existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var t = JToken.ReadFrom(reader);

            var ts = t[nameof(LogEvent.Timestamp)].ToObject<DateTimeOffset>();
            Enum.TryParse(t[nameof(LogEvent.Level)]?.ToString(), out LogEventLevel level);
            var exception = t[nameof(LogEvent.Exception)]?.ToObject<Exception>();
            var template = t[nameof(LogEvent.MessageTemplate)]?.ToObject<MessageTemplate>();
            var props = t[nameof(LogEvent.Properties)]?.ToObject<string[]>()?.Select(_logEventPropertyConverter.DeserializeLogEventProperty);

            return new LogEvent(ts, level, exception, template, props);
        }
    }
}