using Dynamit;

namespace RESTar
{
    [DynamicTable(2), DDictionary(typeof(DynamicResource1KeyValuePair))]
    public class DynamicResource01 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource1KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource1KeyValuePair : DKeyValuePair
    {
        public DynamicResource1KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(2), DDictionary(typeof(DynamicResource2KeyValuePair))]
    public class DynamicResource02 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource2KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource2KeyValuePair : DKeyValuePair
    {
        public DynamicResource2KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(3), DDictionary(typeof(DynamicResource3KeyValuePair))]
    public class DynamicResource03 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource3KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource3KeyValuePair : DKeyValuePair
    {
        public DynamicResource3KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(4), DDictionary(typeof(DynamicResource4KeyValuePair))]
    public class DynamicResource04 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource4KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource4KeyValuePair : DKeyValuePair
    {
        public DynamicResource4KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(5), DDictionary(typeof(DynamicResource5KeyValuePair))]
    public class DynamicResource05 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource5KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource5KeyValuePair : DKeyValuePair
    {
        public DynamicResource5KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(6), DDictionary(typeof(DynamicResource6KeyValuePair))]
    public class DynamicResource06 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource6KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource6KeyValuePair : DKeyValuePair
    {
        public DynamicResource6KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(7), DDictionary(typeof(DynamicResource7KeyValuePair))]
    public class DynamicResource07 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource7KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource7KeyValuePair : DKeyValuePair
    {
        public DynamicResource7KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(8), DDictionary(typeof(DynamicResource8KeyValuePair))]
    public class DynamicResource08 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource8KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource8KeyValuePair : DKeyValuePair
    {
        public DynamicResource8KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(9), DDictionary(typeof(DynamicResource9KeyValuePair))]
    public class DynamicResource09 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource9KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource9KeyValuePair : DKeyValuePair
    {
        public DynamicResource9KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(10), DDictionary(typeof(DynamicResource10KeyValuePair))]
    public class DynamicResource10 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource10KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource10KeyValuePair : DKeyValuePair
    {
        public DynamicResource10KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(11), DDictionary(typeof(DynamicResource11KeyValuePair))]
    public class DynamicResource11 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource11KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource11KeyValuePair : DKeyValuePair
    {
        public DynamicResource11KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(12), DDictionary(typeof(DynamicResource12KeyValuePair))]
    public class DynamicResource12 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource12KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource12KeyValuePair : DKeyValuePair
    {
        public DynamicResource12KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(13), DDictionary(typeof(DynamicResource13KeyValuePair))]
    public class DynamicResource13 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource13KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource13KeyValuePair : DKeyValuePair
    {
        public DynamicResource13KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(14), DDictionary(typeof(DynamicResource14KeyValuePair))]
    public class DynamicResource14 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource14KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource14KeyValuePair : DKeyValuePair
    {
        public DynamicResource14KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(15), DDictionary(typeof(DynamicResource15KeyValuePair))]
    public class DynamicResource15 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource15KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource15KeyValuePair : DKeyValuePair
    {
        public DynamicResource15KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(16), DDictionary(typeof(DynamicResource16KeyValuePair))]
    public class DynamicResource16 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource16KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource16KeyValuePair : DKeyValuePair
    {
        public DynamicResource16KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(17), DDictionary(typeof(DynamicResource17KeyValuePair))]
    public class DynamicResource17 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource17KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource17KeyValuePair : DKeyValuePair
    {
        public DynamicResource17KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(18), DDictionary(typeof(DynamicResource18KeyValuePair))]
    public class DynamicResource18 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource18KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource18KeyValuePair : DKeyValuePair
    {
        public DynamicResource18KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(19), DDictionary(typeof(DynamicResource19KeyValuePair))]
    public class DynamicResource19 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource19KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource19KeyValuePair : DKeyValuePair
    {
        public DynamicResource19KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(20), DDictionary(typeof(DynamicResource20KeyValuePair))]
    public class DynamicResource20 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource20KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource20KeyValuePair : DKeyValuePair
    {
        public DynamicResource20KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(21), DDictionary(typeof(DynamicResource21KeyValuePair))]
    public class DynamicResource21 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource21KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource21KeyValuePair : DKeyValuePair
    {
        public DynamicResource21KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(22), DDictionary(typeof(DynamicResource22KeyValuePair))]
    public class DynamicResource22 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource22KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource22KeyValuePair : DKeyValuePair
    {
        public DynamicResource22KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(23), DDictionary(typeof(DynamicResource23KeyValuePair))]
    public class DynamicResource23 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource23KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource23KeyValuePair : DKeyValuePair
    {
        public DynamicResource23KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(24), DDictionary(typeof(DynamicResource24KeyValuePair))]
    public class DynamicResource24 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource24KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource24KeyValuePair : DKeyValuePair
    {
        public DynamicResource24KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(25), DDictionary(typeof(DynamicResource25KeyValuePair))]
    public class DynamicResource25 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource25KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource25KeyValuePair : DKeyValuePair
    {
        public DynamicResource25KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(26), DDictionary(typeof(DynamicResource26KeyValuePair))]
    public class DynamicResource26 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource26KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource26KeyValuePair : DKeyValuePair
    {
        public DynamicResource26KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(27), DDictionary(typeof(DynamicResource27KeyValuePair))]
    public class DynamicResource27 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource27KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource27KeyValuePair : DKeyValuePair
    {
        public DynamicResource27KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(28), DDictionary(typeof(DynamicResource28KeyValuePair))]
    public class DynamicResource28 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource28KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource28KeyValuePair : DKeyValuePair
    {
        public DynamicResource28KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(29), DDictionary(typeof(DynamicResource29KeyValuePair))]
    public class DynamicResource29 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource29KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource29KeyValuePair : DKeyValuePair
    {
        public DynamicResource29KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(30), DDictionary(typeof(DynamicResource30KeyValuePair))]
    public class DynamicResource30 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource30KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource30KeyValuePair : DKeyValuePair
    {
        public DynamicResource30KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(31), DDictionary(typeof(DynamicResource31KeyValuePair))]
    public class DynamicResource31 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource31KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource31KeyValuePair : DKeyValuePair
    {
        public DynamicResource31KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(32), DDictionary(typeof(DynamicResource32KeyValuePair))]
    public class DynamicResource32 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource32KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource32KeyValuePair : DKeyValuePair
    {
        public DynamicResource32KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(33), DDictionary(typeof(DynamicResource33KeyValuePair))]
    public class DynamicResource33 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource33KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource33KeyValuePair : DKeyValuePair
    {
        public DynamicResource33KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(34), DDictionary(typeof(DynamicResource34KeyValuePair))]
    public class DynamicResource34 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource34KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource34KeyValuePair : DKeyValuePair
    {
        public DynamicResource34KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(35), DDictionary(typeof(DynamicResource35KeyValuePair))]
    public class DynamicResource35 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource35KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource35KeyValuePair : DKeyValuePair
    {
        public DynamicResource35KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(36), DDictionary(typeof(DynamicResource36KeyValuePair))]
    public class DynamicResource36 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource36KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource36KeyValuePair : DKeyValuePair
    {
        public DynamicResource36KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(37), DDictionary(typeof(DynamicResource37KeyValuePair))]
    public class DynamicResource37 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource37KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource37KeyValuePair : DKeyValuePair
    {
        public DynamicResource37KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(38), DDictionary(typeof(DynamicResource38KeyValuePair))]
    public class DynamicResource38 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource38KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource38KeyValuePair : DKeyValuePair
    {
        public DynamicResource38KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(39), DDictionary(typeof(DynamicResource39KeyValuePair))]
    public class DynamicResource39 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource39KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource39KeyValuePair : DKeyValuePair
    {
        public DynamicResource39KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(40), DDictionary(typeof(DynamicResource40KeyValuePair))]
    public class DynamicResource40 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource40KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource40KeyValuePair : DKeyValuePair
    {
        public DynamicResource40KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(41), DDictionary(typeof(DynamicResource41KeyValuePair))]
    public class DynamicResource41 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource41KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource41KeyValuePair : DKeyValuePair
    {
        public DynamicResource41KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(42), DDictionary(typeof(DynamicResource42KeyValuePair))]
    public class DynamicResource42 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource42KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource42KeyValuePair : DKeyValuePair
    {
        public DynamicResource42KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(43), DDictionary(typeof(DynamicResource43KeyValuePair))]
    public class DynamicResource43 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource43KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource43KeyValuePair : DKeyValuePair
    {
        public DynamicResource43KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(44), DDictionary(typeof(DynamicResource44KeyValuePair))]
    public class DynamicResource44 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource44KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource44KeyValuePair : DKeyValuePair
    {
        public DynamicResource44KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(45), DDictionary(typeof(DynamicResource45KeyValuePair))]
    public class DynamicResource45 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource45KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource45KeyValuePair : DKeyValuePair
    {
        public DynamicResource45KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(46), DDictionary(typeof(DynamicResource46KeyValuePair))]
    public class DynamicResource46 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource46KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource46KeyValuePair : DKeyValuePair
    {
        public DynamicResource46KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(47), DDictionary(typeof(DynamicResource47KeyValuePair))]
    public class DynamicResource47 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource47KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource47KeyValuePair : DKeyValuePair
    {
        public DynamicResource47KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(48), DDictionary(typeof(DynamicResource48KeyValuePair))]
    public class DynamicResource48 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource48KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource48KeyValuePair : DKeyValuePair
    {
        public DynamicResource48KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(49), DDictionary(typeof(DynamicResource49KeyValuePair))]
    public class DynamicResource49 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource49KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource49KeyValuePair : DKeyValuePair
    {
        public DynamicResource49KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(50), DDictionary(typeof(DynamicResource50KeyValuePair))]
    public class DynamicResource50 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource50KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource50KeyValuePair : DKeyValuePair
    {
        public DynamicResource50KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(51), DDictionary(typeof(DynamicResource51KeyValuePair))]
    public class DynamicResource51 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource51KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource51KeyValuePair : DKeyValuePair
    {
        public DynamicResource51KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(52), DDictionary(typeof(DynamicResource52KeyValuePair))]
    public class DynamicResource52 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource52KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource52KeyValuePair : DKeyValuePair
    {
        public DynamicResource52KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(53), DDictionary(typeof(DynamicResource53KeyValuePair))]
    public class DynamicResource53 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource53KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource53KeyValuePair : DKeyValuePair
    {
        public DynamicResource53KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(54), DDictionary(typeof(DynamicResource54KeyValuePair))]
    public class DynamicResource54 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource54KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource54KeyValuePair : DKeyValuePair
    {
        public DynamicResource54KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(55), DDictionary(typeof(DynamicResource55KeyValuePair))]
    public class DynamicResource55 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource55KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource55KeyValuePair : DKeyValuePair
    {
        public DynamicResource55KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(56), DDictionary(typeof(DynamicResource56KeyValuePair))]
    public class DynamicResource56 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource56KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource56KeyValuePair : DKeyValuePair
    {
        public DynamicResource56KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(57), DDictionary(typeof(DynamicResource57KeyValuePair))]
    public class DynamicResource57 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource57KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource57KeyValuePair : DKeyValuePair
    {
        public DynamicResource57KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(58), DDictionary(typeof(DynamicResource58KeyValuePair))]
    public class DynamicResource58 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource58KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource58KeyValuePair : DKeyValuePair
    {
        public DynamicResource58KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(59), DDictionary(typeof(DynamicResource59KeyValuePair))]
    public class DynamicResource59 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource59KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource59KeyValuePair : DKeyValuePair
    {
        public DynamicResource59KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(60), DDictionary(typeof(DynamicResource60KeyValuePair))]
    public class DynamicResource60 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource60KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource60KeyValuePair : DKeyValuePair
    {
        public DynamicResource60KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(61), DDictionary(typeof(DynamicResource61KeyValuePair))]
    public class DynamicResource61 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource61KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource61KeyValuePair : DKeyValuePair
    {
        public DynamicResource61KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(62), DDictionary(typeof(DynamicResource62KeyValuePair))]
    public class DynamicResource62 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource62KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource62KeyValuePair : DKeyValuePair
    {
        public DynamicResource62KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(63), DDictionary(typeof(DynamicResource63KeyValuePair))]
    public class DynamicResource63 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource63KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource63KeyValuePair : DKeyValuePair
    {
        public DynamicResource63KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [DynamicTable(64), DDictionary(typeof(DynamicResource64KeyValuePair))]
    public class DynamicResource64 : DDictionary
    {
        protected override DKeyValuePair NewKeyPair(DDictionary dict, string key, object value = null)
        {
            return new DynamicResource64KeyValuePair(dict, key, value);
        }
    }

    public class DynamicResource64KeyValuePair : DKeyValuePair
    {
        public DynamicResource64KeyValuePair(DDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }
}