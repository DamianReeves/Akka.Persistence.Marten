using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Persistence.TestKit.Journal;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace Akka.Persistence.Marten.Tests
{
    [Collection("MartenSpec")]
    public class MartenJournalSpec : JournalSpec
    {
        private static IConfigurationRoot Configuration;
        private static readonly Config SpecConfig;

        static MartenJournalSpec()
        {
            Configuration = TestStartup.CreateConfiguration();

            var connectionString = Configuration?.GetSection("Data:marten")["ConnectionString"];

            SpecConfig = ConfigurationFactory.ParseString($@"
                akka.test.single-expect-default = 3s
                akka.persistence {{
                    publish-plugin-commands = on
                    journal {{
                        plugin = ""akka.persistence.journal.marten""
                        marten {{
                            class = ""Akka.Persistence.Marten.Journal.MartenJournal, Akka.Persistence.Marten""
                            connection-string = ""{connectionString}""
                            plugin-dispatcher = ""akka.actor.default-dispatcher""
                            key-prefix = ""akka:persistence:journal""
                        }}
                    }}
                }}");

        }

        public MartenJournalSpec(ITestOutputHelper output):base(SpecConfig, nameof(MartenJournalSpec),output)
        {
            //var connectionString = SpecConfig.GetString("akka.persistence.journal.marten.connection-string");
            //output.WriteLine($"### ConnectionString: {connectionString}");

            MartenPersistence.Get(Sys);
            Initialize();
        }

        protected override bool SupportsRejectingNonSerializableObjects { get; } = false;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
