using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Serilog.Events;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;
using Xunit;

namespace Serilog.Sink.Cache.Tests
{
    public class CachingSinkTests : IDisposable
    {
        private readonly DatabaseInstance _databaseInstance;
        private readonly Stream _stream;

        public CachingSinkTests()
        {
            _stream = new MemoryStream();
            _databaseInstance = new DatabaseInstance(_stream);
        }

        public void Dispose()
        {
            _stream?.Dispose();
        }

        private CancellationTokenSource _cancellationTokenSource;

        private async Task WaitForSyncProcess(TimeSpan timeout)
        {
            _cancellationTokenSource = new CancellationTokenSource(timeout);

            try
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(0.1), _cancellationTokenSource.Token);
                    if (!_databaseInstance.Any())
                    {
                        return;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"Sync process canceled with {_databaseInstance.Count()} items left in database");
                throw;
            }
        }

        [Theory]
        [InlineData(LogEventLevel.Warning)]
        [InlineData(LogEventLevel.Information)]
        [InlineData(LogEventLevel.Error)]
        [InlineData(LogEventLevel.Fatal)]
        [InlineData(LogEventLevel.Debug)]
        [InlineData(LogEventLevel.Verbose)]
        public async Task LogLevel_LogLevelMaintained(LogEventLevel level)
        {
            // Arrange
            var connectivityMock = new Mock<IConnectivity>();
            // track changes to NetworkAccess and set default value to Internet
            connectivityMock.Setup(con => con.NetworkAccess).Returns(NetworkAccess.Internet);

            var logOnlineSink = new TestableOutputSink();

            var logAlwaysSink = new TestableOutputSink();

            var testableCacheSink = new TestableCachingSink(_databaseInstance, connectivityMock.Object);
            testableCacheSink.AddSink(logOnlineSink);


            using (var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(logAlwaysSink)
                .WriteTo.Sink(testableCacheSink)
                .CreateLogger())
            {
                // Act
                logger.Write(level, "test");

                // give async process some time
                await WaitForSyncProcess(TimeSpan.FromSeconds(10));
            }

            Assert.Equal(level, testableCacheSink.LastLog?.Level);
            Assert.Equal(level, logAlwaysSink.LastLog?.Level);
            Assert.Equal(level, logOnlineSink.LastLog?.Level);
        }


        [Theory]
        [InlineData("test")]
        [InlineData("test", "test2")]
        [InlineData("test", "test2", "test3")]
        public async Task Logs_LogOrderMaintained(params string[] logs)
        {
            // Arrange
            var connectivityMock = new Mock<IConnectivity>();
            // track changes to NetworkAccess and set default value to Internet
            connectivityMock.Setup(con => con.NetworkAccess).Returns(NetworkAccess.Internet);

            var logOnlineSink = new TestableOutputSink();

            var logAlwaysSink = new TestableOutputSink();

            var testableCacheSink = new TestableCachingSink(_databaseInstance, connectivityMock.Object);
            testableCacheSink.AddSink(logOnlineSink);


            using (var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(logAlwaysSink)
                .WriteTo.Sink(testableCacheSink)
                .CreateLogger())
            {
                // Act
                foreach (var log in logs)
                {
                    logger.Write(LogEventLevel.Verbose, log);
                }

                // give async process some time
                await WaitForSyncProcess(TimeSpan.FromSeconds(10));
            }

            // Assert
            // Assert sink log counts
            Assert.Equal(logs.Length, testableCacheSink.Logs.Count);
            Assert.Equal(logs.Length, logAlwaysSink.Logs.Count);
            Assert.Equal(logs.Length, logOnlineSink.Logs.Count);

            // Assert sinks have the same logs
            Assert.Equal(logs, testableCacheSink.Logs.Select(l => l.MessageTemplate.Text));
            Assert.Equal(testableCacheSink.Logs.Select(l => l.MessageTemplate.Text), logAlwaysSink.Logs.Select(l => l.MessageTemplate.Text));
            Assert.Equal(testableCacheSink.Logs.Select(l => l.MessageTemplate.Text), logOnlineSink.Logs.Select(l => l.MessageTemplate.Text));
        }

        [Theory]
        [InlineData("test1 {prop1} {prop2}", 1, 2)]
        [InlineData("test1 {prop1} {prop2}", "1", "2")]
        [InlineData("test1 {prop1} {prop2}", 3f, 4f)]
        [InlineData("test1 {prop1} {prop2}", true, false)]
        public async Task Logs_CorrectProperties_TwoProperties(string log, object prop1, object prop2)
        {
            // Arrange
            var connectivityMock = new Mock<IConnectivity>();
            // track changes to NetworkAccess and set default value to Internet
            connectivityMock.Setup(con => con.NetworkAccess).Returns(NetworkAccess.Internet);

            var logOnlineSink = new TestableOutputSink();

            var logAlwaysSink = new TestableOutputSink();

            var testableCacheSink = new TestableCachingSink(_databaseInstance, connectivityMock.Object);
            testableCacheSink.AddSink(logOnlineSink);

            using (var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(logAlwaysSink)
                .WriteTo.Sink(testableCacheSink)
                .CreateLogger())
            {
                // Act

                logger.Write(LogEventLevel.Verbose, log, prop1, prop2);


                // give async process some time
                await WaitForSyncProcess(TimeSpan.FromSeconds(10));
            }


            // Assert always sink
            Assert.Single(logAlwaysSink.Logs);

            Assert.Equal(log, logAlwaysSink.LastLog.MessageTemplate.Text);

            var alwaysProp1 = Assert.Contains("prop1", logAlwaysSink.LastLog.Properties);
            var alwaysProp2 = Assert.Contains("prop2", logAlwaysSink.LastLog.Properties);

            Assert.Equal(LogEventLevel.Verbose, logAlwaysSink.LastLog.Level);

            // Assert cache
            Assert.Single(testableCacheSink.Logs);

            Assert.Equal(log, testableCacheSink.LastLog.MessageTemplate.Text);

            var cacheProp1 = Assert.Contains("prop1", testableCacheSink.LastLog.Properties);
            var cacheProp2 = Assert.Contains("prop2", testableCacheSink.LastLog.Properties);

            Assert.Equal(LogEventLevel.Verbose, testableCacheSink.LastLog.Level);
            Assert.Equal(alwaysProp1.ToString(), cacheProp1.ToString());
            Assert.Equal(alwaysProp2.ToString(), cacheProp2.ToString());

            // Assert online sink
            Assert.Single(logOnlineSink.Logs);

            Assert.Equal(log, logOnlineSink.LastLog.MessageTemplate.Text);

            var onlineProp1 = Assert.Contains("prop1", logOnlineSink.LastLog.Properties);
            var onlineProp2 = Assert.Contains("prop2", logOnlineSink.LastLog.Properties);

            Assert.Equal(LogEventLevel.Verbose, logOnlineSink.LastLog.Level);
            Assert.Equal(alwaysProp1.ToString(), onlineProp1.ToString());
            Assert.Equal(alwaysProp2.ToString(), onlineProp2.ToString());
        }
    }
}