using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Akka.Persistence.Marten.Tests
{
    [Collection("MartenSpec")]
    public class MartenSettingsSpec : Akka.TestKit.Xunit2.TestKit
    {
        [Fact]
        public void Marten_JournalSettings_must_have_default_values()
        {
            var martenPersistence = MartenPersistence.Get(Sys);

            martenPersistence.JournalSettings.ConnectionString.Should().BeEmpty();
        }

        [Fact]
        public void Marten_SnapshotStoreSettings_must_have_default_values()
        {
            var martenPersistence = MartenPersistence.Get(Sys);

            martenPersistence.SnapshotSettings.ConnectionString.Should().BeEmpty();
        }
    }
}
