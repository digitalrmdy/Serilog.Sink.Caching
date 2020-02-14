using Serilog.Events;

namespace Serilog.Sink.Cache.Model.LogEventPropertyMaps
{
    public interface ILogEventPropertyValueMap
    {
        LogEventPropertyValue ToLogEventPropertyValue();
    }
}