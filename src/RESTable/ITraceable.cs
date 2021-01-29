﻿using RESTable.Requests;

namespace RESTable
{
    /// <summary>
    /// Defines something that can be traced back to an initial message
    /// </summary>
    public interface ITraceable
    {
        /// <summary>
        /// A unique ID
        /// </summary>
        string TraceId { get; }

        /// <summary>
        /// The context to which this trace can be led
        /// </summary>
        RESTableContext Context { get; }
    }
}