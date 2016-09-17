using System;
using Akka.Configuration;
using Marten;

namespace Akka.Persistence.Marten
{
    /// <summary>
    /// Settings for the Marten persistence implementation, parsed from HOCON configuration.
    /// </summary>
    public abstract class MartenSettings
    {
        /// <summary>
        /// Creates a new instance of <see cref="MartenSettings"/>.
        /// </summary>
        /// <param name="config">The HOCON configuration object containing the settings.</param>
        protected MartenSettings(Config config)
        {
            ConnectionString = config?.GetString("connection-string");
            AutoCreateSchemaObjects = GetAutoCreateSetting(config);
        }

        /// <summary>
        /// Gets the connection string used to access Marten (this is a PostgreSql connection string), also specifies the database.
        /// </summary>
        public string ConnectionString { get; }

        /// <summary>
        /// Gets the <see cref="AutoCreate"/> settings for Marten schemas.
        /// </summary>
        public AutoCreate AutoCreateSchemaObjects { get; }

        private AutoCreate GetAutoCreateSetting(Config config)
        {
            var value = AutoCreate.None;
            var setting = config?.GetString("auto-create-schema-objects");
            if (setting != null)
            {
                Enum.TryParse(setting, true, out value);
            }

            return value;            
        }
    }

    /// <summary>
    /// Settings for the Marten journal implementation, parsed from HOCON configuration.
    /// </summary>
    public class MartenJournalSettings : MartenSettings
    {
        /// <summary>
        /// Creates a new instance of <see cref="MartenJournalSettings"/>.
        /// </summary>
        /// <param name="config"></param>
        public MartenJournalSettings(Config config) : base(config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config),
                "Marten journal settings cannot be initialized, because the required HOCON section could not be found.");
        }        
    }

    /// <summary>
    /// Settings for the Marten snapshot store implementation, parsed from HOCON configuration.
    /// </summary>
    public class MartenSnapshotSettings : MartenSettings
    {
        /// <summary>
        /// Creates a new instance of <see cref="MartenSnapshotSettings"/>.
        /// </summary>
        /// <param name="config"></param>
        public MartenSnapshotSettings(Config config) : base(config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config),
                "Marten snapshot store settings cannot be initialized, because the required HOCON section could not be found.");
        }
    }
}