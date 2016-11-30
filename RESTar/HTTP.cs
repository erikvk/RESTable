using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Web;
using Starcounter;

namespace RESTar
{
    public static class HTTP
    {
        #region Http GET

        public static Response GET(string url)
        {
            try
            {
                return Http.GET(url);
            }
            catch (Exception e)
            {
                return null;
            }
        }
        
        #endregion

        #region Http POST

        public static Response POST(string url, string bodyString)
        {
            try
            {
                return Http.POST(url, bodyString, null);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        #endregion

        #region HTTP helper methods

        public static string UrlDecode(string query)
        {
            return HttpUtility.UrlDecode(query);
        }

        public static string GetPrivateIp()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .SelectMany(adapter => adapter.GetIPProperties().UnicastAddresses)
                .Where(adr => adr.Address.AddressFamily == AddressFamily.InterNetwork && adr.IsDnsEligible)
                .Select(adr => adr.Address.ToString()).FirstOrDefault();
        }

        public static string GetPublicIp()
        {
            return GET("http://checkip.dyndns.org").Body
                .Split(':')[1]
                .Substring(1)
                .Split('<')[0];
        }

        #endregion
    }
}