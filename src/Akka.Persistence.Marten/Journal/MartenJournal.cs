using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Persistence.Journal;

namespace Akka.Persistence.Marten.Journal
{
    /// <summary>
    /// An Akka.NET journal implementation that writes events asynchronously to Marten.
    /// </summary>
    public class MartenJournal : AsyncWriteJournal
    {        
        /// <summary>
        /// Creates a new instance of the <see cref="MartenJournal"/>.
        /// </summary>
        public MartenJournal()
        {
            Settings = MartenPersistence.Get(Context.System).JournalSettings;
        }

        internal MartenJournalSettings Settings { get; }

        public override Task ReplayMessagesAsync(IActorContext context, string persistenceId, long fromSequenceNr, long toSequenceNr, long max,
            Action<IPersistentRepresentation> recoveryCallback)
        {
            throw new NotImplementedException();
        }

        public override Task<long> ReadHighestSequenceNrAsync(string persistenceId, long fromSequenceNr)
        {
            throw new NotImplementedException();
        }

        protected override Task<IImmutableList<Exception>> WriteMessagesAsync(IEnumerable<AtomicWrite> messages)
        {
            throw new NotImplementedException();
        }


        protected override Task DeleteMessagesToAsync(string persistenceId, long toSequenceNr)
        {
            throw new NotImplementedException();
        }
    }
}
