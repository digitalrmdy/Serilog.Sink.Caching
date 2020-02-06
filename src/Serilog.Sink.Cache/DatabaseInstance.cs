using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;
using Serilog.Events;

namespace Serilog.Sink.Cache
{
    public class DatabaseInstance
    {
        private LiteDatabase _connection;

        private LiteCollection<LogEntry> LogCollection => _connection?.GetCollection<LogEntry>();

        public DatabaseInstance(string connectionString)
        {
            Connect(connectionString);
        }

        public void StoreLog(LogEvent log)
        {
            if (log == null)
            {
                return;
            }

            LogCollection.Insert(new LogEntry(log));
        }

        public List<LogEvent> GetAllLogs()
        {
            return LogCollection
                .FindAll()
                .Select(entry => entry.LogEvent)
                .OrderBy(log => log.Timestamp)
                .ToList();
        }

        public LogEvent GetNextLog()
        {
            var ts = LogCollection.Min(nameof(LogEntry.Timestamp));

            return LogCollection
                .FindOne(Query.EQ(nameof(LogEntry.Timestamp), ts))
                ?.LogEvent;
        }

        public bool Any()
        {
            return LogCollection != null && LogCollection.Exists(x=>true);
        }

        public void ClearLogs()
        {
            if (LogCollection?.Name == null)
            {
                return;
            }

            try
            {
                _connection.DropCollection(LogCollection?.Name);
            }
            catch
            {
                _connection?.DropCollectionForced(LogCollection?.Name);
            }
        }

        private void Connect(string connectionString)
        {
            _connection = new LiteDatabase(new ConnectionString(connectionString)
            {
                Mode = FileMode.Exclusive,
                Log = Logger.NONE,
                Timeout = TimeSpan.FromSeconds(90),
            });

            EnsureIndices();
        }

        private void EnsureIndices()
        {
            LogCollection?.EnsureIndex(logEvent => logEvent.Timestamp, false);
        }
    }
}