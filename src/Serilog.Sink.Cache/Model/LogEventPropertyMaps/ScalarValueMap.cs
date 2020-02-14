using Serilog.Events;

namespace Serilog.Sink.Cache.Model.LogEventPropertyMaps
{
    public class ScalarValueMap : ILogEventPropertyValueMap
    {
        public object Value { get; set; }

        public ScalarValueMap()
        {
        }

        public LogEventPropertyValue ToLogEventPropertyValue()
        {
            return new ScalarValue(Value);
        }

        public ScalarValue ToScalarValue()
        {
            return ToLogEventPropertyValue() as ScalarValue;
        }

        public static ScalarValueMap FromScalarValue(ScalarValue scalarValue)
        {
            return new ScalarValueMap {Value = scalarValue.Value};
        }
    }
}