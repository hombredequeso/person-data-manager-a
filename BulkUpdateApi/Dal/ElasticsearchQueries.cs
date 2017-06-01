using System;
using System.Collections.Generic;
using System.Linq;
using BulkUpdateApi.Api;
using Elasticsearch.Net;
using Nest;
using Newtonsoft.Json.Linq;

namespace BulkUpdateApi.Dal
{
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

        public static bool IndexPerson(Person person)
        {
            IIndexResponse r = ElasticsearchDb.Client.Index(person, 
                x => x.Index(PersonIndex).Type(PersonType));
            return r.Created;
        }

        public static bool UpdateMatchingPersonTags(BulkTagAdd requestBody)
        {
            PostData<object> bodyx = GetUpdate(
                requestBody.Match.Tags, 
                requestBody.AddTag,
                Guid.NewGuid());
            ElasticsearchResponse<byte[]> response = ElasticsearchDb.Client.LowLevel
                .UpdateByQuery<byte[]>(PersonIndex, bodyx);
            return response.Success;
        }

        public static string SearchPeople(PersonMatch apiSearch)
        {
            ElasticsearchResponse<byte[]> response =
                ElasticsearchDb.Client.LowLevel.Search<byte[]>(new PostData<object>(GetSearchQuery(apiSearch)));
            string jsonResponse = AsUtf8String(response.Body);
            return response.Success ? jsonResponse : null;
        }

        public static string AsUtf8String(byte[] b)
        {
            return System.Text.Encoding.UTF8.GetString(b);
        }

        private static string GetSearchQuery(PersonMatch apiSearch)
        {
            List<JObject> mustClauses = new List<JObject>();
            if (apiSearch.Tags.Any())
                mustClauses.AddRange(apiSearch.Tags.Select(ToTermTag));
            if (!string.IsNullOrWhiteSpace(apiSearch.Name))
                mustClauses.Add(ToMatch("name", apiSearch.Name));

            var mustArrayClauses = new JArray(mustClauses);

            string query = @"
                {
                  ""query"": {
                    ""bool"": {
                      ""must"" : " +
                      mustArrayClauses
                      + @"
                    }
                  }
                }";
            return query;
        }

        public static JObject ToMatch(string field, string value)
        {
            return new JObject
            {
                {
                    "match", new JObject
                    {

                        { field, new JObject
                            {
                                {  "query", value }
                            }
                        }
                    }
                }
            };
        }

        public static JObject ToTermTag(string tag)
        {
            return new JObject
            {
                {
                    "term", new JObject
                    {
                        {"tags", tag}
                    }
                }
            };
        }

        public static JObject GetTag(int tagId, string tagValue)
        {
            return new JObject
            {
                {"id", tagId },
                {"value" , tagValue}
            };
        }

        public static string GetScript()
        {
            var newScript = @"
                ctx._source.tags.add(params.newtag);
                if (ctx._source.operations == null)
                    {ctx._source.operations = [];} 
                ctx._source.operations.add(params.operationId)";
            return newScript.Replace(Environment.NewLine, "");
        }

        public static string GetUpdate(string[] tags, string tagValue, Guid operationId)
        {
            var query = @"
                {
                    ""query"": {
                        ""constant_score"": {
                            ""filter"": {
                              ""bool"": {
                                ""must"":"
                                        + 
                                        new JArray(tags.Select(ToTermTag))
                                        +
                              @"}
                            }
                        }
                    },
                  ""script"": {
                    ""inline"": "" " + GetScript() + @" "",
                    ""lang"": ""painless"",
                    ""params"": {
                      ""newtag"" : """ + tagValue + @""",
                      ""operationId"" : """ + operationId + @"""
                    }
                  }
                }
                ";
            return query;
        }
    }
}