using System;
using System.Collections.Generic;
using LiteDB;
using Serilog.Events;

namespace Serilog.Sink.Cache
{
    public class LogEntry 
    {
        [BsonId]
        public Guid Id { get; set; }

        public LogEvent LogEvent { get; set; }

        public DateTimeOffset Timestamp => LogEvent.Timestamp;

        public LogEntry()
        {
            
        }

        public LogEntry(LogEvent logEvent)
        {
            LogEvent = logEvent;
        }
    }
}