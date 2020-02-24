using System;
using System.IO;
using LiteDB;
using Serilog.Events;
using Serilog.Sink.Cache.Mapping;
using FileMode = LiteDB.FileMode;

namespace Serilog.Sink.Cache
{
    public class DatabaseInstance : IDisposable
    {
        private LiteDatabase _connection;

        private LiteCollection<LogEntry> LogCollection => _connection?.GetCollection<LogEntry>();

        public DatabaseInstance(string connectionString)
        {
            Connect(connectionString);
        }

        public DatabaseInstance(Stream stream)
        {
            Connect(stream);
        }

        public void StoreLog(LogEvent log)
        {
            if (log == null)
            {
                return;
            }

            LogCollection.Insert(new LogEntry(log));
            System.Diagnostics.Debug.WriteLine($"Inserting log     {log.MessageTemplate.Text}");
        }

        public int Count()
        {
            return LogCollection?.Count() ?? -1;
        }

        public LogEntry GetNextLog()
        {
            var ts = LogCollection.Min(nameof(LogEntry.Timestamp));

            return LogCollection
                .FindOne(Query.EQ(nameof(LogEntry.Timestamp), ts));
        }

        public void RemoveLog(LogEntry logEntry)
        {
            LogCollection.Delete(logEntry.Id);
        }

        public bool Any()
        {
            return LogCollection != null && LogCollection.Exists(x => true);
        }

        private void Connect(string connectionString)
        {
            _connection = new LiteDatabase(new ConnectionString(connectionString)
            {
                Mode = FileMode.Exclusive,
                Log = Logger.NONE,
                Timeout = TimeSpan.FromSeconds(90)
            });

            EnsureIndices();
            MapperConfig.Configure();
        }

        private void Connect(Stream memoryStream)
        {
            _connection = new LiteDatabase(memoryStream);
            EnsureIndices();
            MapperConfig.Configure();
        }

        private void EnsureIndices()
        {
            LogCollection?.EnsureIndex(logEvent => logEvent.Id);
            LogCollection?.EnsureIndex(logEvent => logEvent.Timestamp);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connection?.Dispose();
            }
        }
    }
}