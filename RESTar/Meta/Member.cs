using System;

namespace RESTar.Meta
{
    /// <summary>
    /// Encodes a member in a type
    /// </summary>
    public abstract class Member
    {
        /// <summary>
        /// The name of the property
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// The name of the property, as defined in the type declaration
        /// </summary>
        public string ActualName { get; internal set; }

        /// <summary>
        /// The name to use in Starcounter view models
        /// </summary>
        public string ViewModelName => Writable ? Name + "$" : Name;

        /// <summary>
        /// The type of this member
        /// </summary>
        public Type Type { get; protected set; }

        /// <summary>
        /// Is this property nullable?
        /// </summary>
        public bool Nullable { get; protected set; }

        /// <summary>
        /// Is the type an enum type?
        /// </summary>
        public bool IsEnum { get; protected set; }

        /// <summary>
        /// Is this member readable?
        /// </summary>
        public abstract bool Readable { get; }

        /// <summary>
        /// Is this member writable?
        /// </summary>
        public abstract bool Writable { get; }

        /// <summary>
        /// Is this member read-only?
        /// </summary>
        public bool ReadOnly => Readable && !Writable;

        /// <summary>
        /// Is this member write-only?
        /// </summary>
        public bool WriteOnly => Writable && !Readable;
    }
}