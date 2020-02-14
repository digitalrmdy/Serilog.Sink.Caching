using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using LiteDB;
using Serilog.Events;

namespace Serilog.Sink.Cache.Model.LogEventPropertyMaps
{
    public class DictionaryValueMap : ILogEventPropertyValueMap
    {
        public Dictionary<ScalarValueMap, ILogEventPropertyValueMap> MappedElements { get; set; }

        [BsonIgnore]
        public IReadOnlyDictionary<ScalarValue, LogEventPropertyValue> Elements
        {
            get
            {
                var dict = MappedElements?.ToDictionary(kvp => kvp.Key.ToScalarValue(), kvp => kvp.Value.ToLogEventPropertyValue());
                return dict == null ? null : new ReadOnlyDictionary<ScalarValue, LogEventPropertyValue>(dict);
            }
            set
            {
                var dict = value?.ToDictionary(kvp => ScalarValueMap.FromScalarValue(kvp.Key), kvp => Map(kvp.Value));
                MappedElements = dict;
            }
        }

        public DictionaryValueMap()
        {
        }

        public LogEventPropertyValue ToLogEventPropertyValue()
        {
            return new DictionaryValue(Elements);
        }

        public static DictionaryValueMap FromDictionaryValue(DictionaryValue dictionaryValue)
        {
            return new DictionaryValueMap {Elements = dictionaryValue.Elements};
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