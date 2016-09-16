using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Akka.Persistence.Marten.Tests
{
    internal static class TestStartup
    {
        public static IConfigurationRoot CreateConfiguration()
        {
            var inmemoryConfig = new Dictionary<string,string>
            {
                {"Data:Default:ConnectionString","host=localhost;database=marten_test;password=postgres;username=postgres"},
                {"Data:marten:ConnectionString","host=localhost;database=marten_test;password=postgres;username=postgres"},
            };
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(inmemoryConfig)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.user.json", optional: true)
                ;

            return builder.Build();
        }
    }
}