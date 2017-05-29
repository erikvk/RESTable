using System;
using Deflector.Dynamic;

namespace RESTar.Internal
{
    public abstract class Property
    {
        public virtual string Name { get; protected set; }
        public virtual string DatabaseQueryName { get; protected set; }
        public abstract bool Dynamic { get; }
        public bool Static => !Dynamic;
        public abstract bool ScQueryable { get; protected set; }

        public dynamic Get(object target) => Getter?.Invoke(target);
        public void Set(object target, dynamic value) => Setter?.Invoke(target, value);

        protected Setter Setter { get; set; }
        protected Getter Getter { get; set; }
        internal bool CanRead => Getter != null;
        internal bool CanWrite => Setter != null;
        internal bool Readonly => CanRead && !CanWrite;

        public static Property Parse(string keyString, Type resource, bool dynamic)
        {
            if (dynamic) return DynamicProperty.Parse(keyString);
            return StaticProperty.Parse(keyString, resource);
        }
    }
}