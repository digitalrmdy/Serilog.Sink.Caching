using System;
using System.Collections.Generic;
using System.IO;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Serilog.Sink.Cache.Tests
{
    public class DatabaseInstanceTests : IDisposable
    {
        public void Dispose()
        {
        }

        [Fact]
        public void CreateDatabaseFromStream()
        {
            // Arrange
            using (var stream = new MemoryStream())
            {
                // Act
                using (var db = new DatabaseInstance(stream))
                {
                    // Assert
                    Assert.NotNull(db);
                }
            }
        }

        [Fact]
        public void CreateDatabaseFromConnectionString()
        {
            // Arrange
            var connectionString = "db-" + Guid.NewGuid();

            //Act
            using (var db = new DatabaseInstance(connectionString))
            {
                // Assert
                Assert.NotNull(db);
            }

            //Cleanup
            if (File.Exists(connectionString))
            {
                File.Delete(connectionString);
            }
        }

        [Fact]
        public void DatabaseInstance_StoreLog_Null()
        {
            //Arrange
            var timestamp = DateTimeOffset.Now;
            using (var stream = new MemoryStream())
            using (var db = new DatabaseInstance(stream))
            {
                // Act
                db.StoreLog(null);

                // Assert
                Assert.False(db.Any());
            }
        }

        [Fact]
        public void DatabaseInstance_StoreLog_1Log()
        {
            //Arrange
            var timestamp = DateTimeOffset.Now;
            var logEvent = new LogEvent(timestamp, LogEventLevel.Debug, null, new MessageTemplate("log", new List<MessageTemplateToken>()), new List<LogEventProperty>());
            using (var stream = new MemoryStream())
            using (var db = new DatabaseInstance(stream))
            {
                // Act
                db.StoreLog(logEvent);

                // Assert
                Assert.Equal(1, db.Count());
            }
        }

        [Fact]
        public void DatabaseInstance_StoreLog_3logs()
        {
            //Arrange
            var timestamp = DateTimeOffset.Now;

            var logEvent1 = new LogEvent(timestamp, LogEventLevel.Debug, null, new MessageTemplate("log1", new List<MessageTemplateToken>()), new List<LogEventProperty>());
            var logEvent2 = new LogEvent(timestamp, LogEventLevel.Debug, null, new MessageTemplate("log2", new List<MessageTemplateToken>()), new List<LogEventProperty>());
            var logEvent3 = new LogEvent(timestamp, LogEventLevel.Debug, null, new MessageTemplate("log3", new List<MessageTemplateToken>()), new List<LogEventProperty>());

            using (var stream = new MemoryStream())
            using (var db = new DatabaseInstance(stream))
            {
                // Act
                db.StoreLog(logEvent1);
                db.StoreLog(logEvent2);
                db.StoreLog(logEvent3);

                // Assert
                Assert.Equal(3, db.Count());
            }
        }

        [Fact]
        public void DatabaseInstance_RetrieveLogs()
        {
            //Arrange
            var timestamp = DateTimeOffset.Now;

            var logEvent1 = new LogEvent(timestamp, LogEventLevel.Debug, null, new MessageTemplate("log1", new List<MessageTemplateToken>()), new List<LogEventProperty>());
            var logEvent2 = new LogEvent(timestamp, LogEventLevel.Debug, null, new MessageTemplate("log2", new List<MessageTemplateToken>()), new List<LogEventProperty>());
            var logEvent3 = new LogEvent(timestamp, LogEventLevel.Debug, null, new MessageTemplate("log3", new List<MessageTemplateToken>()), new List<LogEventProperty>());

            using (var stream = new MemoryStream())
            using (var db = new DatabaseInstance(stream))
            {
                // Act
                db.StoreLog(logEvent1);
                db.StoreLog(logEvent2);
                db.StoreLog(logEvent3);

                var log1 = db.GetNextLog();
                var log1Duplicate = db.GetNextLog();
                db.RemoveLog(log1);
                
                var log2 = db.GetNextLog();
                db.RemoveLog(log2);

                var log3 = db.GetNextLog();
                db.RemoveLog(log3);

                // Assert
                Assert.Equal(0, db.Count());
                
                Assert.Equal(logEvent1.MessageTemplate.Text, log1.LogEvent.MessageTemplate.Text);
                Assert.Equal(logEvent1.MessageTemplate.Text, log1Duplicate.LogEvent.MessageTemplate.Text);
                Assert.Equal(logEvent2.MessageTemplate.Text, log2.LogEvent.MessageTemplate.Text);
                Assert.Equal(logEvent3.MessageTemplate.Text, log3.LogEvent.MessageTemplate.Text);
            }
        }
    }
}