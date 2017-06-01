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
            Func<IndexDescriptor<Person>, IIndexRequest> selector =
                x =>
                {
                    IndexDescriptor<Person> result =  x.Index(PersonIndex).Type(PersonType);
                    if (!string.IsNullOrWhiteSpace(person.Id))
                    {
                        result = result.Id(person.Id);
                    }
                    return result;
                };

            IIndexResponse r = ElasticsearchDb.Client.Index(person, selector);
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
                ElasticsearchDb.Client.LowLevel.Search<byte[]>("person", "person",
                    new PostData<object>(GetSearchQuery(apiSearch)));
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

            List<JObject> filters = new List<JObject>();
            if (apiSearch.Near != null)
            {
                JObject geoFilter = ToGeoDistance(apiSearch.Near.Coord, apiSearch.Near.Distance);
                filters.Add(geoFilter);
            }
            var filterClauses = new JArray(filters);

            string query = @"
                {
                  ""query"": {
                    ""bool"": {
                      ""must"" : " +
                      mustArrayClauses
                      + @",
                    ""filter"" : " + filterClauses + @"
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

        public static JObject ToGeoDistance(Coord geoCoord, decimal distKm)
        {
            return new JObject
            {
                {
                    "geo_distance", new JObject
                    {
                        {"distance" , $"{distKm}km"},
                        {"geo.coord" , new JObject()
                            {
                                {"lat", geoCoord.Lat},
                                {"lon" , geoCoord.Lon}}
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