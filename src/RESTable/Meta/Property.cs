using System;
using RESTable.Requests;

namespace RESTable.Meta
{
    /// <inheritdoc />
    /// <summary>
    /// Describes a property of a resource
    /// </summary>
    public abstract class Property : Member
    {
        /// <summary>
        /// Is this property a dynamic member?
        /// </summary>
        public abstract bool IsDynamic { get; }

        /// <summary>
        /// Is this property a declared member?
        /// </summary>
        public bool IsDeclared => !IsDynamic;

        /// <summary>
        /// The allowed condition operators for this property
        /// </summary>
        public Operators AllowedConditionOperators { get; protected set; } = Operators.All;

        /// <summary>
        /// Gets the value of this property, for a given target object
        /// </summary>
        public virtual object? GetValue(object target) => Getter?.Invoke(target);

        /// <summary>
        /// Sets the value of this property, for a given target object and a given value
        /// </summary>
        public virtual void SetValue(object target, object? value) => Setter?.Invoke(target, value);

        /// <summary>
        /// </summary>
        internal Setter? Setter { get; set; }

        /// <summary>
        /// </summary>
        internal Getter? Getter { get; set; }

        /// <summary>
        /// Is this property marked as read-only?
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <inheritdoc />
        public override bool IsReadable => Getter is not null;

        /// <inheritdoc />
        public override bool IsWritable => !ReadOnly && Setter is not null;

        protected Property(Type? owner) : base(owner) { }
    }
}