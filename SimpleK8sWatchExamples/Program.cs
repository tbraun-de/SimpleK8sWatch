using System;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SimpleK8sWatch;

namespace SimpleK8sWatchExamples
{
    internal static class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            // Configure logging
            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "hh:mm:ss ";
                }).SetMinimumLevel(LogLevel.Debug));
            var logger = loggerFactory.CreateLogger<WatchedResource<V1ConfigMap, V1ConfigMapList>>();

            // Configure Kubernetes client
            const string ns = "test";
            var config = Config.Configuration();
            var k8SConfig = config.GetSection("K8s").Get<K8SConfig>();

            var c = KubernetesClientConfiguration.BuildConfigFromConfigFile(k8SConfig.KubeConfig);
            var k8S = new Kubernetes(c);

            // watch and log k8s events
            var watch = new WatchedResource<V1ConfigMap, V1ConfigMapList>((doWatch, limit) =>
                k8S.ListNamespacedConfigMapWithHttpMessagesAsync(ns, watch: doWatch, limit: limit), logger);
            watch.EntityChanged += (type, entity) =>
            {
                Console.WriteLine($"Entity changed event [{type}]: {entity.Metadata.Name}");
            };
            
            // wait for keypress
            Console.WriteLine("Press ESC to stop");
            do
            {
                while (!Console.KeyAvailable)
                {
                    // Do something
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }
    }
}