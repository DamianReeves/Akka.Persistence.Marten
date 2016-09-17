using System;
using Marten.Schema;

namespace Akka.Persistence.Marten.Journal
{
    /// <summary>
    /// Class used for storing intermediate result of the <see cref="IPersistentRepresentation"/>
    /// </summary>
    internal class JournalEntryAdded
    {
        public string Id { get; set; }
        public string PersistenceId { get; set; }

        public long SequenceNr { get; set; }

        public bool IsDeleted { get; set; }

        public object Payload { get; set; }

        public string Manifest { get; set; }        
    }

    [PropertySearching(PropertySearching.ContainmentOperator)]
    internal class JournalMetadataEntry
    {
        public Guid Id { get; set; }
        [DuplicateField(PgType = "text")]
        public string PersistenceId { get; set; }
        public long SequenceNr { get; set; }

        public void Apply(JournalEntryAdded entryAdded)
        {
            //Id = entryAdded.Id;
            PersistenceId = entryAdded.PersistenceId;

            var seqNr = entryAdded?.SequenceNr ?? 0;
            if (seqNr > SequenceNr)
            {
                SequenceNr = seqNr;
            }
        }
    }    
}