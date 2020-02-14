using System.Collections.Generic;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sink.Cache.Tests
{
    public class TestableOutputSink : ILogEventSink
    {
        public LogEvent LastLog { get; private set; }
        public List<LogEvent> Logs { get; }

        public TestableOutputSink()
        {
            Logs = new List<LogEvent>();
        }
        
        public void Emit(LogEvent logEvent)
        {
            LastLog = logEvent;
            Logs.Add(logEvent);
        }
    }
}