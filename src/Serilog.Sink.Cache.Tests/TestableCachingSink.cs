using System.Collections.Generic;
using Serilog.Events;
using Xamarin.Essentials.Interfaces;

namespace Serilog.Sink.Cache.Tests
{
    public class TestableCachingSink : CachingSink
    {
        public List<LogEvent> Logs { get; }
        public LogEvent LastLog { get; private set; }

        public TestableCachingSink(DatabaseInstance databaseInstance, IConnectivity connectivity) : base(databaseInstance, connectivity)
        {
            Logs = new List<LogEvent>();
        }

        protected override void EmitInternal(LogEvent logEvent)
        {
            LastLog = logEvent;
            Logs.Add(logEvent);
            
            base.EmitInternal(logEvent);
        }
    }
}