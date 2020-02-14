using System;
using LiteDB;
using Serilog.Events;

namespace Serilog.Sink.Cache.Model
{
    public class LogEntry
    {
        [BsonId]
        public Guid Id { get; set; }

        public EventMap EventMap { get; set; }

        public DateTimeOffset Timestamp => EventMap.Timestamp;

        [BsonIgnore]
        public LogEvent LogEvent
        {
            get => EventMap.ToLogEvent();
            set => EventMap = EventMap.FromLogEvent(value);
        }

        public LogEntry()
        {
            Id = Guid.NewGuid();
        }
        
        public LogEntry(LogEvent logEvent) : this()
        {
            LogEvent = logEvent;
        }
    }
}