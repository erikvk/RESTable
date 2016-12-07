using System;
using System.Collections.Generic;
using System.Net;
using Jil;
using Starcounter;

namespace HelpManager
{
    public class HelpManagerApp
    {
        public HelpManagerApp()
        {
            JSON.SetDefaultOptions(Options.ISO8601PrettyPrintIncludeInherited);

            Handle.GET(8011, "/getarticle/{?}", (Request request, string query) =>
                JSON.Serialize<IEnumerable<HelpArticle>>(query == ""
                    ? Db.SQL<HelpArticle>($"SELECT t FROM {typeof(HelpArticle).FullName} t")
                    : Db.SQL<HelpArticle>($"SELECT t FROM {typeof(HelpArticle).FullName} t WHERE t.Topic =?", query)));

            Handle.POST(8010, "/upload", (Request request) => Db.Transact(() =>
            {
                try
                {
                    dynamic json = JSON.DeserializeDynamic(request.Body);
                    var article = Db.SQL<HelpArticle>($"SELECT t FROM {typeof(HelpArticle).FullName} t " +
                                                      "WHERE t.Topic =? ", (string) json.Topic).First
                                  ?? new HelpArticle {Topic = json.Topic};
                    article.Body = json.Body;
                    article.SeeAlso = json.SeeAlso;
                    return HttpStatusCode.OK;
                }
                catch (DeserializationException)
                {
                    return new Response
                    {
                        StatusCode = (ushort) HttpStatusCode.BadRequest,
                        StatusDescription = $"Error while deserializing JSON. Check JSON syntax:\n{request.Body}"
                    };
                }
            }));
        }

        public static void Main()
        {
            new HelpManagerApp();
        }
    }
}