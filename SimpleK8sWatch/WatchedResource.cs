using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;

namespace SimpleK8sWatch
{
    public class WatchedResource<TEntity, TEntityList> : IDisposable,
        IWatchedResource<IMetadata<V1ObjectMeta>> where TEntity : IMetadata<V1ObjectMeta>
        where TEntityList : IItems<TEntity>
    {
        private readonly ILogger _logger;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly Thread _makeSureWatcherIsRunning;

        private readonly Dictionary<string, TEntity> _resources = new();
        private readonly Func<bool, int?, Task<HttpOperationResponse<TEntityList>>> _watcherMethod;
        private TimeSpan _makeSureWatcherIsRunningDelay = TimeSpan.FromSeconds(1);
        private bool _stopThread;
        private Watcher<TEntity> _watcher;

        public WatchedResource(Func<bool, int?, Task<HttpOperationResponse<TEntityList>>> watcherMethod,
            ILogger logger = null, CancellationToken cancellationToken = default)
        {
            _watcherMethod = watcherMethod;
            _logger = logger;
            _makeSureWatcherIsRunning = new Thread(_ =>
            {
                while (!_stopThread && !cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.WaitHandle.WaitOne(_makeSureWatcherIsRunningDelay);
                    try
                    {
                        RefreshAsync().Wait(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex,
                            $"Error while establishing new watch using method {watcherMethod.Method.Name}");
                    }
                }
            });
            _makeSureWatcherIsRunning.Start();
        }

        public void Dispose()
        {
            _stopThread = true;
            _watcher?.Dispose();
            GC.SuppressFinalize(this);
        }

        public event IWatchedResource<IMetadata<V1ObjectMeta>>.EntityChangeEvent EntityChanged;

        public IEnumerable<T> GetAll<T>() where T : IMetadata<V1ObjectMeta>
        {
            return _resources.Values.Cast<T>();
        }

        private async Task RefreshAsync()
        {
            if (_watcher != null) return;

            _logger?.LogDebug($"Checking if entities are available using method {_watcherMethod.Method.Name}");
            var firstEntity = await _watcherMethod.Invoke(false, 1);
            if (firstEntity.Body.Items.Any())
            {
                _logger?.LogDebug($"Establishing new watcher for method {_watcherMethod.Method.Name}");
                var response = await _watcherMethod.Invoke(true, null);
                _watcher = response.Watch<TEntity, TEntityList>((type, item) =>
                    {
                        var changed = false;
                        switch (type)
                        {
                            case WatchEventType.Added:
                            case WatchEventType.Bookmark:
                            case WatchEventType.Modified:
                                changed = !_resources.ContainsKey(item.Name()) ||
                                          !_resources[item.Name()].IsEqualViaJson(item);
                                _resources[item.Name()] = item;
                                break;
                            case WatchEventType.Deleted:
                                if (_resources.ContainsKey(item.Name()))
                                {
                                    changed = true;
                                    _resources.Remove(item.Name());
                                }

                                break;
                            case WatchEventType.Error:
                                _watcher.Dispose();
                                _watcher = null;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(type), type, null);
                        }

                        // ReSharper disable once InvertIf
                        if (changed)
                        {
                            _logger?.LogDebug(
                                $"Emitting entity change event {type}: {item.Metadata.Name} ({item.GetType().Name})");
                            EntityChanged?.Invoke(type, item);
                        }
                    }, exception =>
                    {
                        _logger?.LogError(exception,
                            $"Error while watching for events using method {_watcherMethod.Method.Name}");
                        _watcher.Dispose();
                        _makeSureWatcherIsRunningDelay = TimeSpan.FromSeconds(1);
                        _watcher = null;
                    },
                    () =>
                    {
                        _logger?.LogInformation(
                            $"Connection was closed while watching for events using method {_watcherMethod.Method.Name}");
                        _watcher.Dispose();
                        _makeSureWatcherIsRunningDelay = TimeSpan.FromSeconds(1);
                        _watcher = null;
                    });
            }
            else
            {
                _makeSureWatcherIsRunningDelay = TimeSpan.FromSeconds(10);
            }
        }
    }
}