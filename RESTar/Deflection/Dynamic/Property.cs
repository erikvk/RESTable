using System;

namespace RESTar.Deflection.Dynamic
{
    /// <summary>
    /// Describes a property of a resource
    /// </summary>
    public abstract class Property
    {
        /// <summary>
        /// The name of the property
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// The database query name to use in Starcounter SQL queries
        /// </summary>
        public string DatabaseQueryName { get; internal set; }

        /// <summary>
        /// The name to use in Starcounter view models
        /// </summary>
        public string ViewModelName => Writable ? Name + "$" : Name;

        /// <summary>
        /// Is this property a dynamic member?
        /// </summary>
        public abstract bool Dynamic { get; }

        /// <summary>
        /// Is this property a static member?
        /// </summary>
        public bool Static => !Dynamic;

        /// <summary>
        /// Can this property be referred to in a Starcounter SQL query?
        /// </summary>
        public bool ScQueryable { get; internal set; }

        /// <summary>
        /// Gets the value of this property, for a given target object
        /// </summary>
        public dynamic GetValue(object target) => Getter?.Invoke(target);

        /// <summary>
        /// Sets the value of this property, for a given target object and a given value
        /// </summary>
        public void SetValue(object target, dynamic value) => Setter?.Invoke(target, value);

        /// <summary>
        /// </summary>
        internal Setter Setter { get; set; }

        /// <summary>
        /// </summary>
        internal Getter Getter { get; set; }

        internal bool Readable => Getter != null;
        internal bool Writable => Setter != null;
        internal bool Readonly => Readable && !Writable;
        
        /// <summary>
        /// Parses an input property name string and returns a Property describing the 
        /// corresponding resource property.
        /// </summary>
        public static Property Parse(string keyString, Type resource, bool dynamic)
        {
            if (dynamic) return DynamicProperty.Parse(keyString);
            return StaticProperty.Find(resource, keyString);
        }
    }
}