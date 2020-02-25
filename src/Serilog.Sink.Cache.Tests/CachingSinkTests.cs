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

        private async Task WaitForSyncProcess(TimeSpan timeout, IConnectivity connectivity)
        {
            _cancellationTokenSource = new CancellationTokenSource(timeout);

            try
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(0.1), _cancellationTokenSource.Token);
                    if (!_databaseInstance.Any() || connectivity.NetworkAccess != NetworkAccess.Internet)
                    {
                        return;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine($"Sync process canceled due to timeout");
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
            connectivityMock.Setup(con => con.NetworkAccess).Returns(NetworkAccess.Internet);
            var conn = connectivityMock.Object;

            var logOnlineSink = new TestableOutputSink();

            var logAlwaysSink = new TestableOutputSink();

            var testableCacheSink = new TestableCachingSink(_databaseInstance, conn);
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
                await WaitForSyncProcess(TimeSpan.FromSeconds(10), conn);
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
            connectivityMock.Setup(con => con.NetworkAccess).Returns(NetworkAccess.Internet);
            var conn = connectivityMock.Object;

            var logOnlineSink = new TestableOutputSink();

            var logAlwaysSink = new TestableOutputSink();

            var testableCacheSink = new TestableCachingSink(_databaseInstance, conn);
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
                await WaitForSyncProcess(TimeSpan.FromSeconds(10), conn);
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
            connectivityMock.Setup(con => con.NetworkAccess).Returns(NetworkAccess.Internet);
            var conn = connectivityMock.Object;

            var logOnlineSink = new TestableOutputSink();

            var logAlwaysSink = new TestableOutputSink();

            var testableCacheSink = new TestableCachingSink(_databaseInstance, conn);
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
                await WaitForSyncProcess(TimeSpan.FromSeconds(10), conn);
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

        [Theory]
        [InlineData("one", "two", "three", "four", "five", NetworkAccess.Local)]
        [InlineData("one", "two", "three", "four", "five", NetworkAccess.None)]
        [InlineData("one", "two", "three", "four", "five", NetworkAccess.Unknown)]
        [InlineData("one", "two", "three", "four", "five", NetworkAccess.ConstrainedInternet)]
        public async Task Logs_LogCount_NetworkChange(string log1, string log2, string log3, string log4, string log5, NetworkAccess networkAccess)
        {
            // Arrange
            var connectivityMock = new Mock<IConnectivity>();
            var access = NetworkAccess.Internet;
            connectivityMock.Setup(con => con.NetworkAccess).Returns(() => access);
            var conn = connectivityMock.Object;

            var logOnlineSink = new TestableOutputSink();

            var logAlwaysSink = new TestableOutputSink();

            var testableCacheSink = new TestableCachingSink(_databaseInstance, conn);
            testableCacheSink.AddSink(logOnlineSink);

            using (var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(logAlwaysSink)
                .WriteTo.Sink(testableCacheSink)
                .CreateLogger())
            {
                // Act
                logger.Write(LogEventLevel.Warning, log1);
                logger.Write(LogEventLevel.Warning, log2);
                logger.Write(LogEventLevel.Warning, log3);

                // Wait a bit for the logs to be processed before disabling internet
                await WaitForSyncProcess(TimeSpan.FromSeconds(10), conn);

                access = networkAccess;
                connectivityMock.Raise(c => c.ConnectivityChanged += null, new ConnectivityChangedEventArgs(conn.NetworkAccess, conn.ConnectionProfiles));

                logger.Write(LogEventLevel.Warning, log4);
                logger.Write(LogEventLevel.Warning, log5);

                // give async process some time
                await WaitForSyncProcess(TimeSpan.FromSeconds(10), conn);
            }

            // Assert 
            // Assert sink log counts
            Assert.Equal(5, testableCacheSink.Logs.Count);
            Assert.Equal(5, logAlwaysSink.Logs.Count);
            Assert.Equal(3, logOnlineSink.Logs.Count);

            // Assert log order for online sink
            Assert.Equal(log1, logOnlineSink.Logs[0].MessageTemplate.Text);
            Assert.Equal(log2, logOnlineSink.Logs[1].MessageTemplate.Text);
            Assert.Equal(log3, logOnlineSink.Logs[2].MessageTemplate.Text);
        }

        [Theory]
        [InlineData("log1", "log2", "log3", "log4", "log5")]
        public async Task Logs_LogCount_Reconnect(string log1, string log2, string log3, string log4, string log5)
        {
            // Arrange
            var connectivityMock = new Mock<IConnectivity>();
            var access = NetworkAccess.Internet;
            connectivityMock.Setup(con => con.NetworkAccess).Returns(() => access);
            var conn = connectivityMock.Object;

            var logOnlineSink = new TestableOutputSink();

            var logAlwaysSink = new TestableOutputSink();

            var testableCacheSink = new TestableCachingSink(_databaseInstance, conn);
            testableCacheSink.AddSink(logOnlineSink);

            using (var logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Sink(logAlwaysSink)
                .WriteTo.Sink(testableCacheSink)
                .CreateLogger())
            {
                // Act
                logger.Write(LogEventLevel.Warning, log1);
                logger.Write(LogEventLevel.Warning, log2);
                logger.Write(LogEventLevel.Warning, log3);

                // Wait a bit for the logs to be processed before disabling internet
                await WaitForSyncProcess(TimeSpan.FromSeconds(10), conn);

                access = NetworkAccess.None;
                connectivityMock.Raise(c => c.ConnectivityChanged += null, new ConnectivityChangedEventArgs(conn.NetworkAccess, conn.ConnectionProfiles));

                // give async process some time to stop due to disconnect
                await Task.Delay(3);

                logger.Write(LogEventLevel.Warning, log4);
                logger.Write(LogEventLevel.Warning, log5);

                access = NetworkAccess.Internet;
                connectivityMock.Raise(c => c.ConnectivityChanged += null, new ConnectivityChangedEventArgs(conn.NetworkAccess, conn.ConnectionProfiles));

                // give async process some time
                await WaitForSyncProcess(TimeSpan.FromSeconds(10), conn);
            }

            // Assert 
            // Assert sink log counts
            Assert.Equal(5, testableCacheSink.Logs.Count);
            Assert.Equal(5, logAlwaysSink.Logs.Count);
            Assert.Equal(5, logOnlineSink.Logs.Count);

            // Assert log order for online sink
            Assert.Equal(log1, logOnlineSink.Logs[0].MessageTemplate.Text);
            Assert.Equal(log2, logOnlineSink.Logs[1].MessageTemplate.Text);
            Assert.Equal(log3, logOnlineSink.Logs[2].MessageTemplate.Text);
            Assert.Equal(log4, logOnlineSink.Logs[3].MessageTemplate.Text);
            Assert.Equal(log5, logOnlineSink.Logs[4].MessageTemplate.Text);
        }
    }
}