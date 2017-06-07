using System;

namespace RESTar.Deflection
{
    public abstract class Property
    {
        public virtual string Name { get; protected set; }
        public virtual string DatabaseQueryName { get; protected set; }
        public string ViewModelName => Writable ? Name + "$" : Name;
        public abstract bool Dynamic { get; }
        public bool Static => !Dynamic;
        public abstract bool ScQueryable { get; protected set; }

        public dynamic Get(object target) => Getter?.Invoke(target);
        public void Set(object target, dynamic value) => Setter?.Invoke(target, value);

        protected Setter Setter { get; set; }
        protected Getter Getter { get; set; }
        internal bool Readable => Getter != null;
        internal bool Writable => Setter != null;
        internal bool Readonly => Readable && !Writable;

        public static Property Parse(string keyString, Type resource, bool dynamic)
        {
            if (dynamic) return DynamicProperty.Parse(keyString);
            return StaticProperty.Parse(keyString, resource);
        }
    }
}