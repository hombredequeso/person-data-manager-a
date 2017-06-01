using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                    var result = x.Index(PersonIndex).Type(PersonType);
                    if (!string.IsNullOrWhiteSpace(person.Id))
                    {
                        result = result.Id(person.Id);
                    }
                    return result;
                };

            var r = ElasticsearchDb.Client.Index(person, selector);
            return r.Created;
        }

        public static bool UpdateMatchingPersonTags(BulkTagAdd requestBody)
        {
            PostData<object> bodyx = GetUpdate(
                requestBody.Match.Tags,
                requestBody.AddTag,
                Guid.NewGuid());
            var response = ElasticsearchDb.Client.LowLevel
                .UpdateByQuery<byte[]>(PersonIndex, bodyx);
            return response.Success;
        }

        public static string SearchPeople(PersonMatch apiSearch)
        {
            var response =
                ElasticsearchDb.Client.LowLevel.Search<byte[]>("person", "person",
                    new PostData<object>(GetSearchQuery(apiSearch)));
            var jsonResponse = AsUtf8String(response.Body);
            return response.Success ? jsonResponse : null;
        }

        public static string AsUtf8String(byte[] b)
        {
            return Encoding.UTF8.GetString(b);
        }

        private static string GetSearchQuery(PersonMatch apiSearch)
        {
            var mustClauses = new List<JObject>();
            if (apiSearch.Tags.Any())
                mustClauses.AddRange(apiSearch.Tags.Select(ToTermTag));
            if (!string.IsNullOrWhiteSpace(apiSearch.Name))
                mustClauses.Add(ToMatch("name", apiSearch.Name));

            var mustArrayClauses = new JArray(mustClauses);

            var filters = new List<JObject>();
            if (apiSearch.Near != null)
            {
                var geoFilter = ToGeoDistance(apiSearch.Near.Coord, apiSearch.Near.Distance);
                filters.Add(geoFilter);
            }
            var filterClauses = new JArray(filters);
            var aggregations = GetAggregations(apiSearch);

            var query = @"
                {
                  ""query"": {
                    ""bool"": {
                      ""must"" : " +
                        mustArrayClauses
                        + @",
                    ""filter"" : " + filterClauses + @"
                    }
                  }, 
                  ""aggs"":  "
                    + aggregations + @"
                }";
            return query;
        }

        public static JObject GetAggregations(PersonMatch apiSearch)
        {
            return new JObject
                {
                    {
                        "tagAggregations", new JObject
                        {
                            {
                                "terms", new JObject
                                {
                                    {"field", "tags.keyword"}
                                }
                            }
                        }
                    },
                    {
                        "geoAggregations", new JObject
                        {
                            {
                                "geo_distance", new JObject
                                {
                                    {
                                        "field", "geo.coord"
                                    },
                                    {
                                        "origin", $"{apiSearch.Near.Coord.Lat}, {apiSearch.Near.Coord.Lon}"
                                    },
                                    {
                                        "unit", "km"
                                    },
                                    {
                                        "ranges", new JArray
                                        {
                                            new JObject{{ "to", 10}},
                                            new JObject{{ "from", 10}, {"to", 20} },
                                            new JObject{{ "from", 20}, {"to", 50} },
                                            new JObject{{ "from", 50}, {"to", 500} },
                                            new JObject{{ "from", 500}},
                                        }
                                    }
                                }
                            }
                        }
                    }
                };
        }

        public static JObject ToMatch(string field, string value)
        {
            return new JObject
            {
                {
                    "match", new JObject
                    {
                        {
                            field, new JObject
                            {
                                {"query", value}
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
                        {"distance", $"{distKm}km"},
                        {
                            "geo.coord", new JObject
                            {
                                {"lat", geoCoord.Lat},
                                {"lon", geoCoord.Lon}
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
                {"id", tagId},
                {"value", tagValue}
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