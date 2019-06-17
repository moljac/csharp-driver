//
//  Copyright (C) DataStax, Inc.
//
//  Please see the license for details:
//  http://www.datastax.com/terms/datastax-dse-driver-license-terms
//

using System;

namespace Dse.Mapping.Attributes
{
    /// <summary>
    /// Used to specify the table a POCO maps to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        /// <summary>
        /// The table name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Determines if the table is defined with COMPACT STORAGE
        /// </summary>
        public bool CompactStorage { get; set; }

        /// <summary>
        /// Determines if all the queries generated for this table uses ALLOW FILTERING option
        /// </summary>
        public bool AllowFiltering { get; set; }

        /// <summary>
        /// If the table identifiers are case sensitive (defaults to false)
        /// </summary>
        public bool CaseSensitive { get; set; }

        /// <summary>
        /// Gets or sets the keyspace name. 
        /// Use only if the table you are mapping is in a different keyspace than the current <see cref="ISession"/>.
        /// </summary>
        public string Keyspace { get; set; }

        /// <summary>
        /// Determines if it should only map properties/fields on the POCO that have a <see cref="ColumnAttribute"/>
        /// </summary>
        public bool ExplicitColumns { get; set; }

        /// <summary>
        /// Specifies the table the POCO maps to.
        /// </summary>
        public TableAttribute()
        {
            
        }

        /// <summary>
        /// Specifies the table a POCO maps to.
        /// </summary>
        /// <param name="tableName">The name of the table to map this POCO to.</param>
        public TableAttribute(string tableName)
        {
            Name = tableName;
        }
    }
}
