using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleK8sWatch;
using SimpleK8sWatchExamples;
using Xunit;

namespace SimpleK8sWatchIntegrationTests
{
    public class WatchedResourceTests
    {
        private const string TestNamespace = "test";
        private readonly Kubernetes _k8S;

        public WatchedResourceTests()
        {
            var config = Config.Configuration();
            var k8SConfig = config.GetSection("K8s").Get<K8SConfig>();

            var c = KubernetesClientConfiguration.BuildConfigFromConfigFile(k8SConfig.KubeConfig);
            _k8S = new Kubernetes(c);
            
            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "hh:mm:ss ";
                }).SetMinimumLevel(LogLevel.Debug));
        }

        [Fact]
        public void WatcherInitGivesEvents()
        {
            using var watch = new WatchedResource<V1Namespace, V1NamespaceList>((b, limit) =>
                _k8S.ListNamespaceWithHttpMessagesAsync(watch: b, limit: limit));
            var events = new List<(WatchEventType type, V1Namespace entity)>();
            watch.EntityChanged += (type, entity) => { events.Add((type, (V1Namespace) entity)); };
            Thread.Sleep(5000);
            Assert.NotEmpty(events);
            var namespaces = watch.GetAll<V1Namespace>().ToList();
            Assert.Equal(namespaces.Count, events.Count);
            Assert.All(namespaces, entity => Assert.Contains(events, ev => ev.entity.IsEqualViaJson(entity)));
        }

        [Fact]
        public async Task WatchingEmptyNamespaceWorks()
        {
            // make sure namespace "test" contains no configmaps
            var configmapsToDelete = await _k8S.ListNamespacedConfigMapAsync(TestNamespace);
            foreach (var configmap in configmapsToDelete.Items)
                await _k8S.DeleteNamespacedConfigMapAsync(configmap.Name(), TestNamespace);

            using var watch = new WatchedResource<V1ConfigMap, V1ConfigMapList>((b, limit) =>
                _k8S.ListNamespacedConfigMapWithHttpMessagesAsync(TestNamespace, watch: b, limit: limit));
            var events = new List<(WatchEventType type, V1Namespace entity)>();
            watch.EntityChanged += (type, entity) => { events.Add((type, (V1Namespace) entity)); };
            Thread.Sleep(1000);
            Assert.Empty(events);
            var configmaps = watch.GetAll<V1ConfigMap>().ToList();
            Assert.Empty(configmaps);
        }

        [Fact]
        public async Task WatchingEmptyNamespaceGrowByTwoItemsWorks()
        {
            // make sure namespace "test" contains no configmaps
            var configmapsToDelete = await _k8S.ListNamespacedConfigMapAsync(TestNamespace);
            foreach (var configmap in configmapsToDelete.Items)
                await _k8S.DeleteNamespacedConfigMapAsync(configmap.Name(), TestNamespace);

            using var watch = new WatchedResource<V1ConfigMap, V1ConfigMapList>((b, limit) =>
                _k8S.ListNamespacedConfigMapWithHttpMessagesAsync(TestNamespace, watch: b, limit: limit));
            var events = new List<(WatchEventType type, V1ConfigMap entity)>();
            watch.EntityChanged += (type, entity) => { events.Add((type, (V1ConfigMap) entity)); };
            Thread.Sleep(1000);
            Assert.Empty(events);
            var configmaps = watch.GetAll<V1ConfigMap>().ToList();
            Assert.Empty(configmaps);

            // Now create two configmaps
            var configMap1 = new V1ConfigMap
            {
                Metadata = new V1ObjectMeta
                {
                    Name = "configmap1",
                    NamespaceProperty = TestNamespace
                },
                Data = new Dictionary<string, string>
                {
                    {"test1", "value1"},
                    {"test2", "value2"}
                }
            };
            var configMap2 = new V1ConfigMap
            {
                Metadata = new V1ObjectMeta
                {
                    Name = "configmap2",
                    NamespaceProperty = TestNamespace
                },
                Data = new Dictionary<string, string>
                {
                    {"test3", "value3"},
                    {"test4", "value4"}
                }
            };
            await _k8S.CreateNamespacedConfigMapAsync(configMap1, TestNamespace);
            await _k8S.CreateNamespacedConfigMapAsync(configMap2, TestNamespace);

            Thread.Sleep(11000); // we need two loops and some extra time to detect the new entities
            Assert.Equal(2, events.Count);
            Assert.Contains(events, ev => ev.entity.Metadata.Name == configMap1.Name());
            Assert.Contains(events, ev => ev.entity.Metadata.Name == configMap2.Name());
        }
    }
}