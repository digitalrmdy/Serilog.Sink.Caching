using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using LiteDB;
using Serilog.Events;
using Serilog.Sink.Cache.Model.LogEventPropertyMaps;

namespace Serilog.Sink.Cache.Model
{
    public class EventMap
    {
        public DateTimeOffset Timestamp { get; set; }
        public LogEventLevel LogEventLevel { get; set; }
        public Exception Exception { get; set; }
        public TemplateMap TemplateMap { get; set; }
        public Dictionary<string, ILogEventPropertyValueMap> PropertiesMap { get; set; }

        [BsonIgnore]
        public MessageTemplate MessageTemplate
        {
            get => TemplateMap.ToMessageTemplate();
            set => TemplateMap = TemplateMap.FromMessageTemplate(value);
        }

        [BsonIgnore]
        public IReadOnlyDictionary<string, LogEventPropertyValue> Properties
        {
            get
            {
                var dict = PropertiesMap?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToLogEventPropertyValue());

                return dict == null ? null : new ReadOnlyDictionary<string, LogEventPropertyValue>(dict);
            }
            set
            {
                var dict = value?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => Map(kvp.Value)
                );

                PropertiesMap = dict;
            }
        }

        public EventMap()
        {
        }

        public static EventMap FromLogEvent(LogEvent logEvent)
        {
            return new EventMap
            {
                Timestamp = logEvent.Timestamp,
                LogEventLevel = logEvent.Level,
                Exception = logEvent.Exception,
                MessageTemplate = logEvent.MessageTemplate,
                Properties = logEvent.Properties
            };
        }

        public LogEvent ToLogEvent()
        {
            return new LogEvent(Timestamp, LogEventLevel, Exception, MessageTemplate, Properties.Select(kvp => new LogEventProperty(kvp.Key, kvp.Value)));
        }

        private ILogEventPropertyValueMap Map(LogEventPropertyValue logEventPropertyValue)
        {
            switch (logEventPropertyValue)
            {
                case DictionaryValue dictionaryValue: return DictionaryValueMap.FromDictionaryValue(dictionaryValue);
                case ScalarValue scalarValue: return ScalarValueMap.FromScalarValue(scalarValue);
                case SequenceValue sequenceValue: return SequenceValueMap.FromSequenceValue(sequenceValue);
                case StructureValue structureValue: return StructureValueMap.FromStructureValue(structureValue);
                default: throw new InvalidEnumArgumentException($"{nameof(LogEventPropertyValue)} of type {logEventPropertyValue.GetType().FullName}could not be mapped");
            }
        }
    }
}