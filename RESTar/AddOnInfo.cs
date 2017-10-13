using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTar
{
    public enum RESTarAddOns
    {
        nil,
        SQLite
    }

    public abstract class AddOnInfo
    {
        internal RESTarAddOns AddOn { get; }
    }
}