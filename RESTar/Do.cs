using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RESTar
{
    internal static class Do
    {
        internal static T Try<T>(Func<T> thingy, T defaultValue)
        {
            try
            {
                return thingy.Invoke();
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
