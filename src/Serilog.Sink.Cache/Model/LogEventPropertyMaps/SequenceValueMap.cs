using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using LiteDB;
using Serilog.Events;
using Xamarin.Essentials;

namespace Serilog.Sink.Cache.Model.LogEventPropertyMaps
{
    public class SequenceValueMap : ILogEventPropertyValueMap
    {
        public IEnumerable<ILogEventPropertyValueMap> MappedElements { get; set; }

        [BsonIgnore]
        public IEnumerable<LogEventPropertyValue> Elements
        {
            get => MappedElements?.Select(p => p.ToLogEventPropertyValue());
            set => value?.Select(p => Map(p));
        }

        public LogEventPropertyValue ToLogEventPropertyValue()
        {
            return new SequenceValue(Elements);
        }

        public static SequenceValueMap FromSequenceValue(SequenceValue sequenceValue)
        {
            return new SequenceValueMap{Elements = sequenceValue.Elements};
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