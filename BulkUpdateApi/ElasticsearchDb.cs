using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Elasticsearch.Net;
using Nest;
using Newtonsoft.Json.Linq;

namespace BulkUpdateApi {

    public static class ElasticsearchDb
    {
        public static ElasticClient Client { get; private set; }

        static ElasticsearchDb()
        {
            var node = new Uri("http://localhost:9200");
            var settings = new ConnectionSettings(node);
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

            Client = new ElasticClient(settings);
        }
    }

    public static class ElasticsearchQueries
    {
        public static readonly string PersonIndex = "person";
        public static readonly string PersonType = "person";

        public static Person GetPerson(string id)
        {
            var request = new GetRequest<Person>(PersonIndex, PersonType, id);
            var response = ElasticsearchDb.Client.Get<Person>(request);
            return response.Found ? response.Source : null;
        }

        public static bool CreatePerson(Person person)
        {
            IIndexResponse r = ElasticsearchDb.Client.Index(person, 
                x => x.Index(PersonIndex).Type(PersonType));
            return r.Created;
        }


        public static bool UpdateMatchingPersonTags(UpdatePersonsTagsCommand cmd)
        {
            PostData<object> bodyx = GetUpdate(
                cmd.Matching.TagIds.Select(x => x.Value).ToArray(),
                cmd.NewTag.Id.Value, cmd.NewTag.Value.Value);
            ElasticsearchResponse<byte[]> response = ElasticsearchDb.Client.LowLevel
                .UpdateByQuery<byte[]>(PersonIndex, bodyx);
            return response.Success;
        }

        public static bool UpdateMatchingPersonTags(BulkTagAdd requestBody)
        {
            PostData<object> bodyx = GetUpdate(
                requestBody.Match.Tags, 
                Int32.Parse(requestBody.AddTag.Id), 
                requestBody.AddTag.Value);
            ElasticsearchResponse<byte[]> response = ElasticsearchDb.Client.LowLevel
                .UpdateByQuery<byte[]>(PersonIndex, bodyx);
            return response.Success;
        }

        public static JArray GetTermsFilterArray(int[] termsId)
        {
            IEnumerable<JObject> zz = termsId
                .Select(x => new JObject
                {
                    { "term", new JObject
                        {
                            {"tags.id", x.ToString()}
                        }
                    }
                }
                );
            return new JArray(zz);
        }

        public static JObject GetTag(int tagId, string tagValue)
        {
            return new JObject
            {
                {"id", tagId },
                {"value" , tagValue}
            };
        }

        public static string GetUpdate(int[] tagIds, int tagId, string tagValue)
        {
            var query = @"
                {
                    ""query"": {
                        ""constant_score"": {
                            ""filter"": {
                              ""bool"": {
                                ""must"":"
                                        + 
                                        GetTermsFilterArray(tagIds)
                                        +
                              @"}
                            }
                        }
                    },
                  ""script"": {
                    ""inline"": ""ctx._source.tags.add(params.newtag)"",
                    ""lang"": ""painless"",
                    ""params"": {
                      ""newtag"" :" + GetTag(tagId, tagValue) +
                    @"}
                  }
                }
                ";
            return query;
        }

        public static bool UpdateMatchingPersonTags_ItsWorking(BulkTagAdd requestBody)
        {
            PostData<object> bodyx = $"{{ \"query\": {{ \"match\": {{ \"tags.id\" : \"1\" }} }}, \"script\": {{ \"inline\": \"ctx._source.tags.add(params.newtag)\", \"lang\": \"painless\", \"params\": {{ \"newtag\" : {{\"id\":\"{requestBody.AddTag.Id}\", \"value\":\"{requestBody.AddTag.Value}\"}} }} }}}}";
            var response = ElasticsearchDb.Client.LowLevel
                .UpdateByQuery<byte[]>(PersonIndex, bodyx);
            return response.Success;
        }
    }
}