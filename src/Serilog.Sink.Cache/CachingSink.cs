using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace Serilog.Sink.Cache
{
    public class CachingSink : ILogEventSink, IDisposable
    {
        private const int LOG_PROCESS_TIMEOUT_SECONDS=30;
        
        private readonly DatabaseInstance _cache;
        private readonly List<ILogEventSink> _sinks;
        private readonly SemaphoreSlim _syncProcessSemaphore;

        private bool _isProcessing;

        private LoggerConfiguration _loggerConfiguration;


        public CachingSink(string connectionString)
        {
            _cache = new DatabaseInstance(connectionString);
            _sinks = new List<ILogEventSink>();

            _syncProcessSemaphore = new SemaphoreSlim(1, 1);

            Connectivity.ConnectivityChanged += OnConnectivityChanged;
        }

        public void SetLoggerConfiguration(LoggerConfiguration loggerConfiguration)
        {
            _loggerConfiguration = loggerConfiguration;
        }

        public LoggerConfiguration BuildCaching()
        {
            return _loggerConfiguration;
        }

        public void AddSink(ILogEventSink sink)
        {
            _sinks.Add(sink);
        }

        public void Emit(LogEvent logEvent)
        {
            _cache.StoreLog(logEvent);
            StartProcessLogs();
        }

        public void Dispose()
        {
            Connectivity.ConnectivityChanged -= OnConnectivityChanged;

            if (_sinks == null || _sinks.Count <= 0)
            {
                return;
            }

            foreach (var sink in _sinks)
            {
                if (sink is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _sinks.Clear();
        }

        private void EmitLog(LogEvent logEvent)
        {
            if (_sinks == null || _sinks.Count <= 0)
            {
                return;
            }

            foreach (var sink in _sinks)
            {
                sink.Emit(logEvent);
                StartProcessLogs();
            }
        }

        private void OnConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess == NetworkAccess.Internet)
            {
                StartProcessLogs();
            }
        }

        private void StartProcessLogs()
        {
            if (!_isProcessing)
            {
                Task.Run(ProcessLogs).ConfigureAwait(false);
            }
        }

        private async Task ProcessLogs()
        {
            if (_isProcessing || !CanEmitLogs())
            {
                return;
            }

            await _syncProcessSemaphore.WaitAsync(TimeSpan.FromSeconds(LOG_PROCESS_TIMEOUT_SECONDS));

            _isProcessing = true;

            try
            {
                var logEvent = _cache.GetNextLog();
                if (logEvent != null)
                {
                    EmitLog(logEvent);
                }
            }
            finally
            {
                _isProcessing = false;
                _syncProcessSemaphore.Release();
            }
        }

        private bool CanEmitLogs()
        {
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                return false;
            }

            if (_cache.Any())
            {
                return false;
            }

            return true;
        }
    }
}