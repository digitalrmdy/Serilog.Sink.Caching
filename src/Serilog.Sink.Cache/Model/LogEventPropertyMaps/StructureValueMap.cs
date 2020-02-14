using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using Serilog.Events;
using Xamarin.Essentials;

namespace Serilog.Sink.Cache.Model.LogEventPropertyMaps
{
    public class StructureValueMap : ILogEventPropertyValueMap
    {
        public string TypeTag { get; set; }
        public IEnumerable<LogEventPropertyMap> MappedProperties { get; set; }

        public StructureValueMap()
        {
            
        }
        
        public LogEventPropertyValue ToLogEventPropertyValue()
        {
            return new StructureValue(MappedProperties?.Select(p => p.ToLogEventProperty()), TypeTag);
        }

        public static StructureValueMap FromStructureValue(StructureValue structureValue)
        {
            return new StructureValueMap
            {
                TypeTag = structureValue.TypeTag,
                MappedProperties = structureValue?.Properties?.Select(LogEventPropertyMap.FromLogEventProperty)
            };
        }
    }
}