using Serilog.Configuration;

namespace Serilog.Sink.Cache
{
    public static class SinkExtensions
    {
        public static CachingSink WithCache(this LoggerSinkConfiguration loggerConfiguration, string connectionString)
        {
            var sink = new CachingSink(connectionString);
            var config = loggerConfiguration.Sink(sink);
            sink.SetLoggerConfiguration(config);
            return sink;
        }
    }
}