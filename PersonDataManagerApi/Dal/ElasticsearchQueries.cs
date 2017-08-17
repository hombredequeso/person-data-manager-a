using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using Elasticsearch.Net;
using Hdq.PersonDataManager.Api.Domain;
using Hdq.PersonDataManager.Api.Modules;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NGeoHash;

namespace Hdq.PersonDataManager.Api.Dal
{
    public static class StringExtensions
    {
        public static string Enclose(this string s, string e = "\"")
        {
            return $"{e}{s}{e}";
        }
    }

    public static class QueryAggregations
    {
        public static JProperty GetGeoDistanceAggregation(Coord coord)
        {
            string s = 
                @"{
                    ""geo_distance"" : {
                        ""field"": ""geo.coord"",
                        ""origin"": " + $"{coord.Lat}, {coord.Lon}".Enclose() + @",
                        ""unit"": ""km"",
                        ""ranges"": [
                            {""to"": 10},
                            {""from"": 10, ""to"": 20},
                            {""from"": 20, ""to"": 50},
                            {""from"": 50, ""to"": 500},
                            {""from"": 500}
                        ]
                    }
                }";
            return new JProperty("geoDistance", JObject.Parse(s));
        }

        public static JProperty GetGeoGridAggregation()
        {
            string s =
                @"{
                    ""geohash_grid"" : {
                        ""field"" : ""address.geo.coord"",
                        ""precision"" : 3
                    }
                }";
            return new JProperty("geoGrid", JObject.Parse(s));
        }

        public static JProperty GetTagAggregations()
        {
            string obj = 
                    @"{
                        ""terms"" : {
                            ""field"" : ""tags.keyword""
                        }
                    }";

            return new JProperty("tagAggs", JObject.Parse(obj));
        }
        
        
        public static void AddIf(this JObject o, bool conditional, Func<JProperty> p)
        {
            if (conditional)
                o.Add(p());
        }
        
        
        public static JProperty GetPoolStatusAggregations()
        {
            string obj =
                @"{
                    ""nested"": {
                        ""path"": ""poolStatuses""
                    },
                    ""aggs"": {
                        ""poolStatusAggs"": {
                            ""terms"": {
                                ""field"": ""poolStatuses.pool.id""
                            },
                            ""aggs"": {
                                ""pool.description"": {
                                    ""terms"": {
                                        ""field"": ""poolStatuses.pool.description"",
                                        ""size"": 1
                                    }
                                },
                                ""statusAgg"": {
                                    ""terms"": {
                                        ""field"": ""poolStatuses.status""
                                    }
                                }
                            }
                        }
                    }
                  }";
            
            return new JProperty("poolStatusAggs", JObject.Parse(obj));

        }

    }

    public static class ElasticsearchQueries
    {
        public static bool SearchOnGeoCoord(PersonMatch apiSearch)
        {
            return (apiSearch?.Near?.Coord != null);
        }

        public static readonly string PersonIndex = "person";
        public static readonly string PersonType = "person";

        public static readonly string SavedQueryIndex = "savedquery";
        public static readonly string SavedQueryType = "savedquery";

        public static Person GetPerson(string id)
        {
            var request = new GetRequest<Person>(PersonIndex, PersonType, id);
            IGetResponse<Person> response = ElasticsearchDb.Client.Get<Person>(request);
            return response.Found ? response.Source : null;
        }

        public static JObject GetQuery(string id)
        {
            ElasticsearchResponse<byte[]> response = ElasticsearchDb.Client.LowLevel.Get<byte[]>(
                SavedQueryIndex, SavedQueryType, id);
            if (response.Success)
            {
                var respBody = JObject.Parse(AsUtf8String(response.Body));
                var isFound = (bool)respBody["found"];
                if (isFound)
                {
                    var result = (JObject)respBody["_source"];
                    return result;
                }
                return null;
            }
            return null;
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

            IIndexResponse r = ElasticsearchDb.Client.Index(person, selector);
            return r.IsValid;
        }

        public static bool IndexQuery(SavedQuery savedQuery)
        {
            // TODO: convert SavedQuery data into an actual query form here, and index that.
            string bodyContent = GetSavedQueryBody(savedQuery).ToString(Formatting.None);
            PostData<object> body = bodyContent;
            string id = savedQuery.Metadata.Id;
            ElasticsearchResponse<byte[]> response = ElasticsearchDb.Client.LowLevel.Index<byte[]>(
                SavedQueryIndex,
                SavedQueryType,
                id,
                body);
            return response.Success;
        }

        private static JObject GetSavedQueryBody(SavedQuery savedQuery)
        {
            var mustClauses = new List<JObject>();
            if (savedQuery.QueryParameters.Tags.Any())
                mustClauses.AddRange(savedQuery.QueryParameters.Tags.Select(ToTermTag));

            var mustArrayClauses = new JArray(mustClauses);
            JsonSerializerSettings settings =
                new JsonSerializerSettings {ContractResolver = new CamelCasePropertyNamesContractResolver()};
            JsonSerializer serializer = JsonSerializer.CreateDefault(settings);
            var sq = JObject.FromObject(savedQuery, serializer);

            var result =  JObject.Parse(@"{
                    ""query"": {
                        ""bool"": {
                          ""must"" : " + mustArrayClauses
                        + @"
                        }
                    }
                }");
            result.Merge(sq);
            return result;
        }

        public class CommandResponse
        {
            public CommandResponse(Guid commandId, bool success, IEnumerable<string>  message = null)
            {
                CommandId = commandId;
                Success = success;
                Message = message?.ToArray() ?? new string[0];
            }

            public Guid CommandId { get; }
            public bool Success { get; }
            public string[] Message { get; }
        }
        
        public static CommandResponse UpdateMatchingPersonTags(Command<BulkTagAdd> cmd)
        {
            PostData<object> body = GetUpdate(
                cmd.Cmd.Match.Tags,
                cmd.Cmd.Match.Name.FirstName,
                cmd.Cmd.AddTag,
                cmd.Id);
            ElasticsearchResponse<byte[]> response = ElasticsearchDb.Client.LowLevel
                .UpdateByQuery<byte[]>(PersonIndex, body);
            var commandResponse = ToCommandResponse(cmd, response);
            return commandResponse;
        }

        private static CommandResponse ToCommandResponse(
            Command<BulkTagAdd> cmd, 
            ElasticsearchResponse<byte[]> response)
        {
            JObject responseBody = JObject.Parse(AsUtf8String(response.Body));
            Console.WriteLine(responseBody.ToString());
            if (((JArray)responseBody["failures"]).Any())
            {
                return new CommandResponse(
                    cmd.Id, 
                    false, 
                    new []{"Some entities failed to update"});
            }
            return new CommandResponse(cmd.Id, response.Success);
        }

        public static JObject ToJObject(GeohashDecodeResult geoHashResult)
        {
            JObject result = new JObject(
                new JProperty("lat", geoHashResult.Coordinates.Lat),
                new JProperty("lon", geoHashResult.Coordinates.Lon)
                );
            return result;
        }

        public static JToken GetToken(JToken start, string[] elements)
        {
            if (elements.Length == 0)
                return start;
            JToken nextToken = start[elements[0]];
            if (nextToken == null)
                return null;
            return GetToken(nextToken, elements.Skip(1).ToArray());
        }

        public static string EnrichEsSearchResult(byte[] searchResult)
        {
            string result =  AsUtf8String(searchResult);
            JObject resultAsJObject = JObject.Parse(result);
            JToken geoBuckets =  GetToken(
                resultAsJObject, 
                new []{"aggregations", "geoGrid", "buckets"});
            if (geoBuckets == null)
                return result;

            foreach (JToken bucket in geoBuckets)
            {
                var geoHash = (string)bucket["key"];
                GeohashDecodeResult coords = GeoHash.Decode(geoHash);
                ((JObject)bucket).Add("coord", ToJObject(coords));
            }
            var serializedResult = resultAsJObject.ToString();
            return serializedResult;
        }

        public static string SearchPeople(PersonMatch apiSearch, int from, int size)
        {
            var response =
                ElasticsearchDb.Client.LowLevel.Search<byte[]>("person", "person",
                    new PostData<object>(GetSearchQuery(apiSearch, from, size)));

            return response.Success 
                ? EnrichEsSearchResult(response.Body) 
                : null;
        }


        public static string SearchSavedQueries(SavedQueryMatch apiSearch, int from, int size)
        {
            var response = ElasticsearchDb.Client.LowLevel.Search<byte[]>(
                "savedquery",
                "savedquery",
                new PostData<object>(GetSavedQueriesSearchQuery(apiSearch, from, size)));

            return AsUtf8String(response.Body);
        }

        public static string PercolateSearchSavedQueries(SavedQueryPercolateMatch search, int from, int size)
        {
            var query = @"
                {
                  ""query"": {
                    ""percolate"": {
                        ""field"": ""query"",
                        ""document_type"": ""person"",
                        ""index"": ""person"",
                        ""type"": ""person"",
                        ""id"": """ + search.Id + @"""
                    }
                  } 
                }";

            var response =
                ElasticsearchDb.Client.LowLevel.Search<byte[]>("savedquery", "savedquery",
                    new PostData<object>(query));

            return response.Success 
                ? AsUtf8String(response.Body)
                : null;
        }

        private static string GetSavedQueriesSearchQuery(SavedQueryMatch apiSearch, int from, int size)
        {

            var query = @"
                {
                  ""from"": " + from + @",
                  ""size"": " + size + @"
                }";
            return query;
        }


        public static string MoreLikePeople(string[] ids)
        {
            var response =
                ElasticsearchDb.Client.LowLevel.Search<byte[]>("person", "person",
                    new PostData<object>(GetMoreLikeQuery(ids)));
            var jsonResponse = AsUtf8String(response.Body);
            return response.Success ? jsonResponse : null;
        }

        private static string GetMoreLikeQuery(string[] ids)
        {
            var aggs = new JObject();
            aggs.Add(QueryAggregations.GetTagAggregations());
            var query = @"
                {
                  ""query"": {
                    ""more_like_this"": {
                      ""fields"" : [""tags""],
                        ""like"" : " + GetIdArray(ids) + @",
                        ""min_term_freq"":  1,
                        ""max_query_terms"": 12
                    }
                  },
                  ""aggs"":  "
                    + aggs + @"
                }";
            return query;
        }

        public static JArray GetIdArray(string[] ids)
        {
            var result = ids.Select(x => new JObject
            {
                {"_index", "person"},
                {"_type", "person"},
                {"_id", x}
            });
            return new JArray(result);
        }

        public static string AsUtf8String(byte[] b)
        {
            return Encoding.UTF8.GetString(b);
        }

        private static string GetSearchQuery(PersonMatch apiSearch, int from, int size)
        {
            var mustClauses = new List<JObject>();
            if (apiSearch.Tags.Any())
                mustClauses.AddRange(apiSearch.Tags.Select(ToTermTag));
            if (apiSearch.PoolStatuses.Any())
                mustClauses.AddRange(apiSearch.PoolStatuses.Select(ToTermPool));
            if (apiSearch.Name != null)
            {
                if (!string.IsNullOrWhiteSpace(apiSearch.Name.FirstName))
                    mustClauses.Add(ToMatch("name.firstName", apiSearch.Name.FirstName));
                if (!string.IsNullOrWhiteSpace(apiSearch.Name.LastName))
                    mustClauses.Add(ToMatch("name.lastName", apiSearch.Name.LastName));
            }

            var mustArrayClauses = new JArray(mustClauses);

            var filters = new List<JObject>();
            if (apiSearch.Near != null)
            {
                var geoFilter = ToGeoDistance(apiSearch.Near.Coord, apiSearch.Near.Distance);
                filters.Add(geoFilter);
            }
            var filterClauses = new JArray(filters);
            
            var aggregations = GetAggregations(apiSearch);
            var postFilterArrayClauses = GetPostFilterArrayClauses(apiSearch);

            var query = @"
                {
                  ""from"": " + from + @",
                  ""size"": " + size + @",
                  ""query"": {
                    ""bool"": {
                      ""must"" : " +
                        mustArrayClauses
                        + @",
                    ""filter"" : " + filterClauses + @"
                    }
                  }, 
                  ""aggs"":  "
                    + aggregations + @",
                  ""post_filter"": {
                        ""bool"": {
                          ""must"" : " +
                        postFilterArrayClauses
                        + @"
                    }
                  }
                }";
            return query;
        }

        private static JArray GetPostFilterArrayClauses(PersonMatch apiSearch)
        {
            var postFilterClauses = new List<JObject>();
            if (apiSearch.PostFilter != null && apiSearch.PostFilter.Tags.Any())
                postFilterClauses.AddRange(apiSearch.PostFilter.Tags.Select(ToTermTag));
            var postFilterArrayClauses = new JArray(postFilterClauses);
            return postFilterArrayClauses;
        }


        private static JObject GetAggregations(PersonMatch apiSearch)
        {
            return new JObject
            {
                QueryAggregations.GetPoolStatusAggregations(),
                QueryAggregations.GetTagAggregations(),
                SearchOnGeoCoord(apiSearch)
                    ? QueryAggregations.GetGeoDistanceAggregation(apiSearch.Near.Coord)
                    : QueryAggregations.GetGeoGridAggregation()
            };
        }

        public static JObject ToMatch(string field, string value)
        {
            string obj = 
                @"{
                    ""match"" : {
                        " + field.Enclose() + @": {
                            ""query"": " + value.Enclose() + @"
                        }
                    }
                }";
            return JObject.Parse(obj);
        }

        public static JObject ToGeoDistance(Coord geoCoord, decimal distKm)
        {
            string obj = 
                @"{
                    ""geo_distance"" : {
                        ""distance"" : " + $"{distKm}km".Enclose() + @",
                        ""address.geo.coord"" : {
                            ""lat"": " + geoCoord.Lat + @",
                            ""lon"": " + geoCoord.Lon + @"
                        }
                    }
                }";
            return JObject.Parse(obj);
        }

        public static JObject ToTermTag(string tag)
        {
            return JObject.Parse(
                @"{
                    ""match"" : {
                        ""tags"": " + tag.Enclose() + @"
                    }
                }"
            );
        }
        
        public static JObject ToTermPool(PoolStatus poolStatus)
        {
            var poolClause = 
            JObject.Parse(
                @"{
                    ""match"" : {
                        ""poolStatuses.pool.id"": " + poolStatus.Pool.Id.ToString().Enclose() + @"
                    }
                }"
            );
            if (string.IsNullOrWhiteSpace(poolStatus.Status))
            {
                return ToNested("poolStatuses", new[] {poolClause});
            }
            
            var statusClause = 
                JObject.Parse(
                    @"{
                        ""match"" : {
                            ""poolStatuses.status"": " + poolStatus.Status.Enclose() + @"
                        }
                    }"
                );
            return ToNested("poolStatuses", new[] {poolClause, statusClause});
        }

        private static JObject ToNested(string poolstatuses, JObject[] searchClauses)
        {
            return new JObject(
                new JProperty("nested", new JObject
                {
                    new JProperty("path", poolstatuses),
                    new JProperty("query", new JObject
                    {
                        new JProperty("bool", new JObject
                        {
                            new JProperty("must", new JArray(searchClauses))
                        })
                    })
                }));
        }

        public static JObject ToTerm(string field, string value)
        {
            return JObject.Parse(
                @"{
                    ""match"" : {
                        """ + field + @""": " + value.Enclose() + @"
                    }
                }"
            );
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

        public static string GetUpdate(
            string[] tags, 
            string firstName,
            string tagValue, Guid operationId)
        {
            var filterTerms = tags.Select(ToTermTag).ToList();
            if (!string.IsNullOrWhiteSpace(firstName))
            {
                var nameTerm = ToTerm("name.firstName", firstName);
                filterTerms.Add(nameTerm);
            }
             
            var query = @"
                {
                    ""conflicts"": ""proceed"",
                    ""query"": {
                        ""constant_score"": {
                            ""filter"": {
                              ""bool"": {
                                ""must"":"
                        +
                        new JArray(filterTerms)
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