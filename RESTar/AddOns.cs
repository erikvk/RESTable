using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using static System.Reflection.BindingFlags;
using static RESTar.RESTarAddOns;

namespace RESTar
{
    internal static class AddOns
    {
        internal static void Init(IEnumerable<AddOnInfo> addOns)
        {
            if (addOns == null) return;
            foreach (var addOn in addOns)
            {
                switch (addOn.AddOn)
                {
                    case nil: return;
                    case SQLite:
                        InitSQLiteAddOn(addOn);
                        break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static void InitSQLiteAddOn(AddOnInfo addOn)
        {
            Assembly sqliteAssembly;
            const string assemblyName = "RESTar.SQLite.dll";
            try
            {
                var path = Path.GetDirectoryName(
                    Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
                sqliteAssembly = Assembly.LoadFile($@"{path}\{assemblyName}");
            }
            catch
            {
                throw new RESTarAddOnException($"Could not set up add-on '{addOn.AddOn}'. Could not find the " +
                                               $"'{assemblyName}' assembly. Is there a reference to the add-on " +
                                               "assembly from your application?");
            }

            sqliteAssembly
                .GetType("RESTar.SQLite.SQLiteConfig")
                .GetMethod("Init", NonPublic | Static)
                .Invoke(null, new object[] {addOn});
        }
    }
}