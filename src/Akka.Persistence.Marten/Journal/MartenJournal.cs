using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Persistence.Journal;
using Akka.Serialization;
using Marten;
using Marten.Events;
using Marten.Linq.SoftDeletes;

namespace Akka.Persistence.Marten.Journal
{
    /// <summary>
    /// An Akka.NET journal implementation that writes events asynchronously to Marten.
    /// </summary>
    public class MartenJournal : AsyncWriteJournal
    {
        private Lazy<DocumentStore> _store;
        //private Lazy<Serializer> _serializer;

        /// <summary>
        /// Creates a new instance of the <see cref="MartenJournal"/>.
        /// </summary>
        public MartenJournal()
        {
            Settings = MartenPersistence.Get(Context.System).JournalSettings;
        }

        internal MartenJournalSettings Settings { get; }

        protected override void PreStart()
        {
            base.PreStart();
            _store = new Lazy<DocumentStore>(() =>
            {
                return DocumentStore.For(_ =>
                {
                    _.Connection(Settings.ConnectionString);
                    //
                    _.AutoCreateSchemaObjects = Settings.AutoCreateSchemaObjects;
                });
            });

            //_serializer = new Lazy<Serializer>(() => _system.Serialization.FindSerializerForType(typeof(JournalEntry)));
        }

        public override async Task ReplayMessagesAsync(IActorContext context, string persistenceId, long fromSequenceNr, long toSequenceNr, long max,
            Action<IPersistentRepresentation> recoveryCallback)
        {
            // Limit allows only integer
            var limitValue = max >= int.MaxValue ? int.MaxValue : (int)max;
            var stream = PersistenceIdToStream(persistenceId);
            using (var session = _store.Value.LightweightSession())
            {
                //var streamState = session.Events.FetchStreamState(stream);
                //streamState.
                var events = await session.Events.QueryAllRawEvents()
                    .Where(x => x.StreamId == stream)
                    .Where(x => x.Version >= fromSequenceNr)
                    .Where(x => x.Version <= toSequenceNr)
                    .Take(limitValue)
                    .ToListAsync();

                foreach (var @event in events)
                {
                    //@event.Data
                }
            }
        }

        public override async Task<long> ReadHighestSequenceNrAsync(string persistenceId, long fromSequenceNr)
        {
            var stream = PersistenceIdToStream(persistenceId);
            using (var session = _store.Value.OpenSession())
            {
                var state = await session.Events.FetchStreamStateAsync(stream);
                return state.Version;
            }
        }

        protected override async Task<IImmutableList<Exception>> WriteMessagesAsync(IEnumerable<AtomicWrite> messages)
        {
            var exceptions = ImmutableList<Exception>.Empty;
            using (var session = _store.Value.OpenSession())
            {
                try
                {
                    foreach (var message in messages)
                    {
                        var stream = PersistenceIdToStream(message.PersistenceId);
                        var persistRepresentation = (IPersistentRepresentation) message.Payload;
                        //TODO: We need to work through situations where the serializer is not sending JSON
                        var eventData = persistRepresentation.Payload; //Assuming JSON for now
                        session.Events.Append(stream, eventData);
                        session.Events.S
                    }

                    await session.SaveChangesAsync();

                }
                catch (Exception ex)
                {
                    exceptions = exceptions.Add(ex);
                }
            }
            return exceptions;
        }


        protected override async Task DeleteMessagesToAsync(string persistenceId, long toSequenceNr)
        {
            var stream = PersistenceIdToStream(persistenceId);
            using (var session = _store.Value.LightweightSession())
            {
                var evt = await session.Events.QueryAllRawEvents()
                    .Where(x => x.StreamId == stream)
                    .Where(x => x.Version == toSequenceNr)
                    .FirstOrDefaultAsync();

                if (evt != null)
                {
                    session.Delete(evt);
                }
            }
        }

        private Guid PersistenceIdToStream(string persistenceId) => GuidUtility.Create(GuidUtility.IsoOidNamespace, persistenceId);
    }
}
