using System;
using Dynamit;
using Starcounter;

namespace RESTar.Dynamit
{
    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit1 : ScDictionary
    {
        public Dynamit1() : base(typeof(Dynamit1KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit1KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit1KeyValuePair : ScKeyValuePair
    {
        public Dynamit1KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit2 : ScDictionary
    {
        public Dynamit2() : base(typeof(Dynamit2KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit2KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit2KeyValuePair : ScKeyValuePair
    {
        public Dynamit2KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit3 : ScDictionary
    {
        public Dynamit3() : base(typeof(Dynamit3KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit3KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit3KeyValuePair : ScKeyValuePair
    {
        public Dynamit3KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit4 : ScDictionary
    {
        public Dynamit4() : base(typeof(Dynamit4KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit4KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit4KeyValuePair : ScKeyValuePair
    {
        public Dynamit4KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit5 : ScDictionary
    {
        public Dynamit5() : base(typeof(Dynamit5KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit5KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit5KeyValuePair : ScKeyValuePair
    {
        public Dynamit5KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit6 : ScDictionary
    {
        public Dynamit6() : base(typeof(Dynamit6KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit6KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit6KeyValuePair : ScKeyValuePair
    {
        public Dynamit6KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit7 : ScDictionary
    {
        public Dynamit7() : base(typeof(Dynamit7KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit7KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit7KeyValuePair : ScKeyValuePair
    {
        public Dynamit7KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit8 : ScDictionary
    {
        public Dynamit8() : base(typeof(Dynamit8KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit8KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit8KeyValuePair : ScKeyValuePair
    {
        public Dynamit8KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit9 : ScDictionary
    {
        public Dynamit9() : base(typeof(Dynamit9KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit9KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit9KeyValuePair : ScKeyValuePair
    {
        public Dynamit9KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit10 : ScDictionary
    {
        public Dynamit10() : base(typeof(Dynamit10KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit10KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit10KeyValuePair : ScKeyValuePair
    {
        public Dynamit10KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit11 : ScDictionary
    {
        public Dynamit11() : base(typeof(Dynamit11KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit11KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit11KeyValuePair : ScKeyValuePair
    {
        public Dynamit11KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit12 : ScDictionary
    {
        public Dynamit12() : base(typeof(Dynamit12KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit12KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit12KeyValuePair : ScKeyValuePair
    {
        public Dynamit12KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit13 : ScDictionary
    {
        public Dynamit13() : base(typeof(Dynamit13KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit13KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit13KeyValuePair : ScKeyValuePair
    {
        public Dynamit13KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit14 : ScDictionary
    {
        public Dynamit14() : base(typeof(Dynamit14KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit14KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit14KeyValuePair : ScKeyValuePair
    {
        public Dynamit14KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit15 : ScDictionary
    {
        public Dynamit15() : base(typeof(Dynamit15KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit15KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit15KeyValuePair : ScKeyValuePair
    {
        public Dynamit15KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit16 : ScDictionary
    {
        public Dynamit16() : base(typeof(Dynamit16KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit16KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit16KeyValuePair : ScKeyValuePair
    {
        public Dynamit16KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit17 : ScDictionary
    {
        public Dynamit17() : base(typeof(Dynamit17KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit17KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit17KeyValuePair : ScKeyValuePair
    {
        public Dynamit17KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit18 : ScDictionary
    {
        public Dynamit18() : base(typeof(Dynamit18KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit18KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit18KeyValuePair : ScKeyValuePair
    {
        public Dynamit18KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit19 : ScDictionary
    {
        public Dynamit19() : base(typeof(Dynamit19KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit19KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit19KeyValuePair : ScKeyValuePair
    {
        public Dynamit19KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit20 : ScDictionary
    {
        public Dynamit20() : base(typeof(Dynamit20KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit20KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit20KeyValuePair : ScKeyValuePair
    {
        public Dynamit20KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit21 : ScDictionary
    {
        public Dynamit21() : base(typeof(Dynamit21KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit21KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit21KeyValuePair : ScKeyValuePair
    {
        public Dynamit21KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit22 : ScDictionary
    {
        public Dynamit22() : base(typeof(Dynamit22KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit22KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit22KeyValuePair : ScKeyValuePair
    {
        public Dynamit22KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit23 : ScDictionary
    {
        public Dynamit23() : base(typeof(Dynamit23KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit23KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit23KeyValuePair : ScKeyValuePair
    {
        public Dynamit23KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit24 : ScDictionary
    {
        public Dynamit24() : base(typeof(Dynamit24KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit24KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit24KeyValuePair : ScKeyValuePair
    {
        public Dynamit24KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit25 : ScDictionary
    {
        public Dynamit25() : base(typeof(Dynamit25KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit25KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit25KeyValuePair : ScKeyValuePair
    {
        public Dynamit25KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit26 : ScDictionary
    {
        public Dynamit26() : base(typeof(Dynamit26KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit26KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit26KeyValuePair : ScKeyValuePair
    {
        public Dynamit26KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit27 : ScDictionary
    {
        public Dynamit27() : base(typeof(Dynamit27KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit27KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit27KeyValuePair : ScKeyValuePair
    {
        public Dynamit27KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit28 : ScDictionary
    {
        public Dynamit28() : base(typeof(Dynamit28KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit28KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit28KeyValuePair : ScKeyValuePair
    {
        public Dynamit28KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit29 : ScDictionary
    {
        public Dynamit29() : base(typeof(Dynamit29KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit29KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit29KeyValuePair : ScKeyValuePair
    {
        public Dynamit29KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit30 : ScDictionary
    {
        public Dynamit30() : base(typeof(Dynamit30KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit30KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit30KeyValuePair : ScKeyValuePair
    {
        public Dynamit30KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit31 : ScDictionary
    {
        public Dynamit31() : base(typeof(Dynamit31KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit31KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit31KeyValuePair : ScKeyValuePair
    {
        public Dynamit31KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit32 : ScDictionary
    {
        public Dynamit32() : base(typeof(Dynamit32KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit32KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit32KeyValuePair : ScKeyValuePair
    {
        public Dynamit32KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit33 : ScDictionary
    {
        public Dynamit33() : base(typeof(Dynamit33KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit33KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit33KeyValuePair : ScKeyValuePair
    {
        public Dynamit33KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit34 : ScDictionary
    {
        public Dynamit34() : base(typeof(Dynamit34KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit34KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit34KeyValuePair : ScKeyValuePair
    {
        public Dynamit34KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit35 : ScDictionary
    {
        public Dynamit35() : base(typeof(Dynamit35KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit35KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit35KeyValuePair : ScKeyValuePair
    {
        public Dynamit35KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit36 : ScDictionary
    {
        public Dynamit36() : base(typeof(Dynamit36KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit36KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit36KeyValuePair : ScKeyValuePair
    {
        public Dynamit36KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit37 : ScDictionary
    {
        public Dynamit37() : base(typeof(Dynamit37KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit37KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit37KeyValuePair : ScKeyValuePair
    {
        public Dynamit37KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit38 : ScDictionary
    {
        public Dynamit38() : base(typeof(Dynamit38KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit38KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit38KeyValuePair : ScKeyValuePair
    {
        public Dynamit38KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit39 : ScDictionary
    {
        public Dynamit39() : base(typeof(Dynamit39KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit39KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit39KeyValuePair : ScKeyValuePair
    {
        public Dynamit39KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit40 : ScDictionary
    {
        public Dynamit40() : base(typeof(Dynamit40KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit40KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit40KeyValuePair : ScKeyValuePair
    {
        public Dynamit40KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit41 : ScDictionary
    {
        public Dynamit41() : base(typeof(Dynamit41KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit41KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit41KeyValuePair : ScKeyValuePair
    {
        public Dynamit41KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit42 : ScDictionary
    {
        public Dynamit42() : base(typeof(Dynamit42KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit42KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit42KeyValuePair : ScKeyValuePair
    {
        public Dynamit42KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit43 : ScDictionary
    {
        public Dynamit43() : base(typeof(Dynamit43KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit43KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit43KeyValuePair : ScKeyValuePair
    {
        public Dynamit43KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit44 : ScDictionary
    {
        public Dynamit44() : base(typeof(Dynamit44KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit44KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit44KeyValuePair : ScKeyValuePair
    {
        public Dynamit44KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit45 : ScDictionary
    {
        public Dynamit45() : base(typeof(Dynamit45KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit45KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit45KeyValuePair : ScKeyValuePair
    {
        public Dynamit45KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit46 : ScDictionary
    {
        public Dynamit46() : base(typeof(Dynamit46KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit46KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit46KeyValuePair : ScKeyValuePair
    {
        public Dynamit46KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit47 : ScDictionary
    {
        public Dynamit47() : base(typeof(Dynamit47KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit47KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit47KeyValuePair : ScKeyValuePair
    {
        public Dynamit47KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit48 : ScDictionary
    {
        public Dynamit48() : base(typeof(Dynamit48KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit48KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit48KeyValuePair : ScKeyValuePair
    {
        public Dynamit48KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit49 : ScDictionary
    {
        public Dynamit49() : base(typeof(Dynamit49KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit49KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit49KeyValuePair : ScKeyValuePair
    {
        public Dynamit49KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit50 : ScDictionary
    {
        public Dynamit50() : base(typeof(Dynamit50KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit50KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit50KeyValuePair : ScKeyValuePair
    {
        public Dynamit50KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit51 : ScDictionary
    {
        public Dynamit51() : base(typeof(Dynamit51KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit51KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit51KeyValuePair : ScKeyValuePair
    {
        public Dynamit51KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit52 : ScDictionary
    {
        public Dynamit52() : base(typeof(Dynamit52KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit52KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit52KeyValuePair : ScKeyValuePair
    {
        public Dynamit52KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit53 : ScDictionary
    {
        public Dynamit53() : base(typeof(Dynamit53KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit53KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit53KeyValuePair : ScKeyValuePair
    {
        public Dynamit53KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit54 : ScDictionary
    {
        public Dynamit54() : base(typeof(Dynamit54KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit54KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit54KeyValuePair : ScKeyValuePair
    {
        public Dynamit54KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit55 : ScDictionary
    {
        public Dynamit55() : base(typeof(Dynamit55KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit55KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit55KeyValuePair : ScKeyValuePair
    {
        public Dynamit55KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit56 : ScDictionary
    {
        public Dynamit56() : base(typeof(Dynamit56KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit56KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit56KeyValuePair : ScKeyValuePair
    {
        public Dynamit56KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit57 : ScDictionary
    {
        public Dynamit57() : base(typeof(Dynamit57KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit57KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit57KeyValuePair : ScKeyValuePair
    {
        public Dynamit57KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit58 : ScDictionary
    {
        public Dynamit58() : base(typeof(Dynamit58KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit58KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit58KeyValuePair : ScKeyValuePair
    {
        public Dynamit58KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit59 : ScDictionary
    {
        public Dynamit59() : base(typeof(Dynamit59KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit59KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit59KeyValuePair : ScKeyValuePair
    {
        public Dynamit59KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit60 : ScDictionary
    {
        public Dynamit60() : base(typeof(Dynamit60KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit60KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit60KeyValuePair : ScKeyValuePair
    {
        public Dynamit60KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit61 : ScDictionary
    {
        public Dynamit61() : base(typeof(Dynamit61KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit61KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit61KeyValuePair : ScKeyValuePair
    {
        public Dynamit61KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit62 : ScDictionary
    {
        public Dynamit62() : base(typeof(Dynamit62KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit62KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit62KeyValuePair : ScKeyValuePair
    {
        public Dynamit62KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit63 : ScDictionary
    {
        public Dynamit63() : base(typeof(Dynamit63KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit63KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit63KeyValuePair : ScKeyValuePair
    {
        public Dynamit63KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }

    [Database, RESTar(RESTarPresets.ReadAndWrite)]
    public class Dynamit64 : ScDictionary
    {
        public Dynamit64() : base(typeof(Dynamit64KeyValuePair))
        {
        }

        protected override ScKeyValuePair NewKeyPair(ScDictionary dict, string key, object value = null)
        {
            return new Dynamit64KeyValuePair(dict, key, value);
        }
    }

    public class Dynamit64KeyValuePair : ScKeyValuePair
    {
        public Dynamit64KeyValuePair(ScDictionary dict, string key, object value = null) : base(dict, key, value)
        {
        }
    }
}