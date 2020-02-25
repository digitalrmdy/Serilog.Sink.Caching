using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;
using Xamarin.Essentials.Implementation;

namespace Serilog.Sink.Cache
{
    public class CachingSink : ILogEventSink, IDisposable
    {
        private const int LOG_PROCESS_TIMEOUT_SECONDS = 30;

        private readonly DatabaseInstance _cache;
        private readonly List<ILogEventSink> _sinks;
        private readonly IConnectivity _connectivity;
        private readonly SemaphoreSlim _syncProcessSemaphore;

        private bool _isProcessing;

        private LoggerConfiguration _loggerConfiguration;

        public CachingSink(string connectionString, IConnectivity connectivity = null) : this(new DatabaseInstance(connectionString), connectivity)
        {
        }

        public CachingSink(DatabaseInstance databaseInstance, IConnectivity connectivity = null)
        {
            _cache = databaseInstance;
            _sinks = new List<ILogEventSink>();
            _connectivity = connectivity ?? new ConnectivityImplementation();

            _syncProcessSemaphore = new SemaphoreSlim(1, 1);

            _connectivity.ConnectivityChanged += OnConnectivityChanged;
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
            EmitInternal(logEvent);
        }

        protected virtual void EmitInternal(LogEvent logEvent)
        {
            _cache.StoreLog(logEvent);
            StartProcessLogs();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connectivity.ConnectivityChanged -= OnConnectivityChanged;

                if (_sinks != null && _sinks.Any())
                {
                    foreach (var sink in _sinks)
                    {
                        if (sink is IDisposable disposable)
                        {
                            disposable.Dispose();
                        }
                    }

                    _sinks.Clear();
                }

                _cache?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

        protected virtual async Task ProcessLogs()
        {
            if (_isProcessing || !CanEmitLogs())
            {
                return;
            }

            await _syncProcessSemaphore.WaitAsync(TimeSpan.FromSeconds(LOG_PROCESS_TIMEOUT_SECONDS));

            _isProcessing = true;

            try
            {
                var logEntry = _cache.GetNextLog();
                if (logEntry?.LogEvent != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Emitting log:    {logEntry.LogEvent.MessageTemplate.Text}");
                    EmitLog(logEntry.LogEvent);
                    _cache.RemoveLog(logEntry);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
            }
            finally
            {
                _isProcessing = false;
                _syncProcessSemaphore.Release();
            }

            StartProcessLogs();
        }

        private bool CanEmitLogs()
        {
            if (_connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                return false;
            }

            if (!_cache.Any())
            {
                return false;
            }

            return true;
        }
    }
}