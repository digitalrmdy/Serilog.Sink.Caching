using System.ComponentModel;
using LiteDB;
using Serilog.Events;
using Xamarin.Essentials;

namespace Serilog.Sink.Cache.Model.LogEventPropertyMaps
{
    public class LogEventPropertyMap
    {
        public string Name { get; set; }

        public ILogEventPropertyValueMap LogEventPropertyValueMap { get; set; }

        [BsonIgnore]
        public LogEventPropertyValue Value
        {
            get => LogEventPropertyValueMap.ToLogEventPropertyValue();
            set => LogEventPropertyValueMap = Map(value);
        }

        public LogEventPropertyMap()
        {
        }

        public static LogEventPropertyMap FromLogEventProperty(LogEventProperty logEventProperty)
        {
            return new LogEventPropertyMap
            {
                Name = logEventProperty.Name,
                Value = logEventProperty.Value
            };
        }

        public LogEventProperty ToLogEventProperty()
        {
            return new LogEventProperty(Name, Value);
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