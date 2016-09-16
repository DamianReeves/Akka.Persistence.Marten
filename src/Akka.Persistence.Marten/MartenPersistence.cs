using System;
using Akka.Actor;
using Akka.Configuration;

namespace Akka.Persistence.Marten
{
    /// <summary>
    /// An actor system extension initializing support for the Marten persistence layer.
    /// </summary>
    public class MartenPersistence : IExtension
    {        
        /// <summary>
        /// Creates a new instance of the Marten persistence extension.
        /// </summary>
        /// <param name="system"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public MartenPersistence(ExtendedActorSystem system)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));

            // Initialize fallback configuration defaults
            var defaultConfig = DefaultConfig();            
            system.Settings.InjectTopLevelFallback(defaultConfig);

            // Read config
            var journalConfig = system.Settings.Config.GetConfig("akka.persistence.journal.marten");
            JournalSettings = new MartenJournalSettings(journalConfig);

            var snapshotConfig = system.Settings.Config.GetConfig("akka.persistence.snapshot-store.marten");
            SnapshotSettings = new MartenSnapshotSettings(snapshotConfig);

        }

        /// <summary>
        /// Get the journal settings for the persistence extension.
        /// </summary>
        public MartenJournalSettings JournalSettings { get; }

        /// <summary>
        /// Get the snapshot settings for the persistence extension.
        /// </summary>
        public MartenSnapshotSettings SnapshotSettings { get; }

        /// <summary>
        /// Get the Marten persistence extension for the provided <see cref="ActorSystem"/>.
        /// </summary>
        /// <param name="system">The actor system.</param>
        /// <returns>The Marten persistence extension.</returns>
        public static MartenPersistence Get(ActorSystem system)
            => system.WithExtension<MartenPersistence, MartenPersistenceProvider>();

        /// <summary>
        /// Get the default configuration for the persistence provider.
        /// </summary>
        /// <returns></returns>
        public static Config DefaultConfig()
            => ConfigurationFactory.FromResource<MartenPersistence>("Akka.Persistence.Marten.marten.conf");
    }

    /// <summary>
    /// Extension Id provider for the Marten persistence extension.
    /// </summary>
    public class MartenPersistenceProvider : ExtensionIdProvider<MartenPersistence>
    {
        /// <summary>
        /// Creates an actor system extension for the akka persistence Marten support.
        /// </summary>
        /// <param name="system">The actor system to extend.</param>
        /// <returns>The newly created actor system extension.</returns>
        public override MartenPersistence CreateExtension(ExtendedActorSystem system)
        {
            return new MartenPersistence(system);
        }
    }
}
