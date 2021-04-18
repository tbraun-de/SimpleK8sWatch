using Microsoft.Extensions.Configuration;

// ReSharper disable ClassNeverInstantiated.Global

namespace SimpleK8sWatchExamples
{
    public class Config
    {
        public static IConfiguration Configuration()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
            return config;
        }
    }
}