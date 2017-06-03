using System;
using System.Diagnostics;
using System.Text;
using Nest;

namespace Hdq.PersonDataManager.Api.Dal
{
    public static class ElasticsearchDb
    {
        public static ElasticClient Client { get; private set; }

        static ElasticsearchDb()
        {
            var node = new Uri("http://localhost:9200");
            var settings = new ConnectionSettings(node);
            WriteElasticsearchRequestsToDebug(settings);
            Client = new ElasticClient(settings);
        }

        private static void WriteElasticsearchRequestsToDebug(ConnectionSettings settings)
        {
            settings.DisableDirectStreaming()
                .OnRequestCompleted(details =>
                {
                    Debug.WriteLine("### ES REQEUST ###");
                    if (details.RequestBodyInBytes != null)
                        Debug.WriteLine(Encoding.UTF8.GetString(details.RequestBodyInBytes));
                    Debug.WriteLine("### ES RESPONSE ###");
                    if (details.ResponseBodyInBytes != null)
                        Debug.WriteLine(Encoding.UTF8.GetString(details.ResponseBodyInBytes));
                })
                .PrettyJson();
        }
    }
}