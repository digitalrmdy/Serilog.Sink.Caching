using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteDB;
using Serilog.Events;
using Serilog.Sink.Cache.Model;
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

        public List<LogEvent> GetAllLogs()
        {
            return LogCollection
                .FindAll()
                .Select(entry => entry.LogEvent)
                .OrderBy(log => log.Timestamp)
                .ToList();
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

        public int Count()
        {
            return LogCollection?.Count() ?? 0;
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
            ConfigureBsonMapper();
        }

        private void Connect(Stream memoryStream)
        {
            _connection = new LiteDatabase(memoryStream);
            EnsureIndices();
            ConfigureBsonMapper();
        }

        private void EnsureIndices()
        {
            LogCollection?.EnsureIndex(logEvent => logEvent.Timestamp, false);
        }

        private void ConfigureBsonMapper()
        {
            BsonMapper.Global.EmptyStringToNull = false;
            BsonMapper.Global.TrimWhitespace = false;
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}