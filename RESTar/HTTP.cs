using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Web;
using Starcounter;

namespace RESTar
{
    internal static class HTTP
    {
        internal static Response GET(string url)
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

        internal static Response INNER(string method, string url, string body)
        {
            try
            {
                if (!url.Contains("http"))
                    url = "http://" + url;
                return Http.CustomRESTRequest(method, url, body, new Dictionary<string, string>());
            }
            catch (Exception e)
            {
                return null;
            }
        }

        internal static Response POST(string url, string bodyString)
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

        internal static string UrlDecode(string query)
        {
            return HttpUtility.UrlDecode(query);
        }

        internal static string GetPrivateIp()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .SelectMany(adapter => adapter.GetIPProperties().UnicastAddresses)
                .Where(adr => adr.Address.AddressFamily == AddressFamily.InterNetwork && adr.IsDnsEligible)
                .Select(adr => adr.Address.ToString()).FirstOrDefault();
        }

        internal static string GetPublicIp()
        {
            return GET("http://checkip.dyndns.org").Body
                .Split(':')[1]
                .Substring(1)
                .Split('<')[0];
        }
    }
}