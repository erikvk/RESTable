namespace RESTable.Sqlite
{
    /// <summary>
    /// Allowed CLR data types for mapping with Sqlite tables
    /// </summary>
    public enum ClrDataType
    {
        /// <summary />
        Unsupported = 0,

        /// <summary />
        Int16,

        /// <summary />
        Int32,

        /// <summary />
        Int64,

        /// <summary />
        Single,

        /// <summary />
        Double,

        /// <summary />
        Decimal,

        /// <summary />
        Byte,

        /// <summary />
        String,

        /// <summary />
        Boolean,

        /// <summary />
        DateTime,
    }
}