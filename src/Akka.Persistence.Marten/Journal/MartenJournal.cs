using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
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
        private Lazy<Serializer> _serializer;

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

                    _.Events.AddEventType(typeof(JournalEntryAdded));
                    _.Events.InlineProjections.AggregateStreamsWith<JournalMetadataEntry>();
                });
            });

            _serializer = new Lazy<Serializer>(() => Context.System.Serialization.FindSerializerForType(typeof(JournalEntryAdded)));
        }

        public override async Task ReplayMessagesAsync(IActorContext context, string persistenceId, long fromSequenceNr, long toSequenceNr, long max,
            Action<IPersistentRepresentation> recoveryCallback)
        {
            // Limit allows only integer
            var limitValue = max >= int.MaxValue ? int.MaxValue : (int)max;
            // Do not replay messages if limit equal zero
            if (limitValue == 0)
                return;

            var stream = PersistenceIdToStream(persistenceId);
            using (var session = _store.Value.LightweightSession())
            {
                //var streamState = session.Events.FetchStreamState(stream);
                //streamState.
                var events = await session.Events.QueryRawEventDataOnly<JournalEntryAdded>()
                    .Where(x=>x.PersistenceId == persistenceId && x.SequenceNr >= fromSequenceNr && x.SequenceNr <= toSequenceNr && x.IsDeleted == false)
                    .Take(limitValue)
                    .ToListAsync();

                foreach (var @event in events)
                {
                    var persistent = ToPersistenceRepresentation(@event, context.Sender);
                    recoveryCallback(persistent);
                }
            }
        }

        public override async Task<long> ReadHighestSequenceNrAsync(string persistenceId, long fromSequenceNr)
        {
            var stream = PersistenceIdToStream(persistenceId);
            using (var session = _store.Value.OpenSession())
            {
                var metadata = await session.Query<JournalMetadataEntry>()
                    .FirstOrDefaultAsync(x=>x.PersistenceId == persistenceId);

                return metadata?.SequenceNr ?? 0;

                //var state = await session.Events.FetchStreamStateAsync(stream);
                //return state.Version;
            }
        }

        protected override async Task<IImmutableList<Exception>> WriteMessagesAsync(IEnumerable<AtomicWrite> messages)
        {
            var messageList = messages.ToList();
            var groupedTasks = messageList.GroupBy(x => x.PersistenceId).ToDictionary(g => g.Key, async g =>
            {
                var persistentMessages = g.SelectMany(aw => (IImmutableList<IPersistentRepresentation>)aw.Payload).ToList();
                var eventsToSave = persistentMessages.Select(ToJournalEntryAdded).Cast<object>().ToArray();

                var persistenceId = g.Key;
                var stream = PersistenceIdToStream(persistenceId);

                using (var session = _store.Value.OpenSession())
                {
                    session.Events.Append(stream, eventsToSave);
                    await session.SaveChangesAsync();
                }
            });

            return await Task<IImmutableList<Exception>>.Factory.ContinueWhenAll(
                    groupedTasks.Values.ToArray(),
                    tasks => messageList.Select(
                        m =>
                        {
                            var task = groupedTasks[m.PersistenceId];
                            return task.IsFaulted ? TryUnwrapException(task.Exception) : null;
                        }).ToImmutableList());
        }


        protected override async Task DeleteMessagesToAsync(string persistenceId, long toSequenceNr)
        {
            var stream = PersistenceIdToStream(persistenceId);
            using (var session = _store.Value.LightweightSession())
            {
                var events = await session.Events.QueryRawEventDataOnly<JournalEntryAdded>()
                    .Where(e=>e.SequenceNr <= toSequenceNr)
                    .ToListAsync();

                foreach (var @event in events)
                {
                    //session.Delete(@event);
                    @event.IsDeleted = true;
                }

                await session.SaveChangesAsync();
            }
        }

        private Guid PersistenceIdToStream(string persistenceId) => GuidUtility.Create(GuidUtility.IsoOidNamespace, persistenceId);
        private JournalEntryAdded ToJournalEntryAdded(IPersistentRepresentation message)
        {
            return new JournalEntryAdded
            {
                Id = message.PersistenceId + "_" + message.SequenceNr,
                IsDeleted = message.IsDeleted,
                Payload = message.Payload,
                PersistenceId = message.PersistenceId,
                SequenceNr = message.SequenceNr,
                Manifest = message.Manifest
            };
        }

        private Persistent ToPersistenceRepresentation(JournalEntryAdded entryAdded, IActorRef sender)
        {
            return new Persistent(entryAdded.Payload, entryAdded.SequenceNr, entryAdded.PersistenceId, entryAdded.Manifest, entryAdded.IsDeleted, sender);
        }        
    }
}
