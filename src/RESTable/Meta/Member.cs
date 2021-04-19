using System;
using System.Collections.Generic;
using RESTable.Resources;

namespace RESTable.Meta
{
    /// <summary>
    /// Represents a member in a type
    /// </summary>
    public abstract class Member
    {
        /// <summary>
        /// The name of the property
        /// </summary>
        [RESTableMember(order: -5)]
        public string Name { get; internal set; }

        /// <summary>
        /// The type that owns this member
        /// </summary>
        public Type Owner { get; }
        
        /// <summary>
        /// The name of the property, as defined in the type declaration
        /// </summary>
        public string ActualName { get; internal set; }

        /// <summary>
        /// The type of this member
        /// </summary>
        public Type Type { get; protected set; }

        /// <summary>
        /// Is this property nullable?
        /// </summary>
        public bool IsNullable { get; protected set; }

        /// <summary>
        /// Is the type an enum type?
        /// </summary>
        public bool IsEnum { get; protected set; }

        /// <summary>
        /// Is this member readable?
        /// </summary>
        public abstract bool IsReadable { get; }

        /// <summary>
        /// Is this member writable?
        /// </summary>
        public abstract bool IsWritable { get; }

        /// <summary>
        /// Is this member read-only?
        /// </summary>
        public bool IsReadOnly => IsReadable && !IsWritable;

        /// <summary>
        /// Is this member write-only?
        /// </summary>
        public bool IsWriteOnly => IsWritable && !IsReadable;

        /// <summary>
        ///  Custom flags added to this member
        /// </summary>
        public HashSet<string> Flags { get; }

        /// <inheritdoc />
        public override string ToString() => Name;

        protected Member(Type owner)
        {
            Owner = owner;
            Flags = new HashSet<string>();
        }
    }
}