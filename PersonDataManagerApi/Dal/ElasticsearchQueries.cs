using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Elasticsearch.Net;
using Hdq.PersonDataManager.Api.Domain;
using Hdq.PersonDataManager.Api.Modules;
using Nest;
using Newtonsoft.Json.Linq;
using NGeoHash;

namespace Hdq.PersonDataManager.Api.Dal
{
    public static class ElasticsearchQueries
    {
        public static bool SearchOnGeoCoord(PersonMatch apiSearch)
        {
            return (apiSearch?.Near?.Coord != null);
        }

        public static readonly string PersonIndex = "person";
        public static readonly string PersonType = "person";

        public static Person GetPerson(string id)
        {
            var request = new GetRequest<Person>(PersonIndex, PersonType, id);
            IGetResponse<Person> response = ElasticsearchDb.Client.Get<Person>(request);
            return response.Found ? response.Source : null;
        }

        public static bool IndexPerson(Person person, bool refresh)
        {
            Func<IndexDescriptor<Person>, IIndexRequest> selector =
                x =>
                {
                    var result = x.Index(PersonIndex).Type(PersonType);
                    if (!string.IsNullOrWhiteSpace(person.Id))
                    {
                        result = result.Id(person.Id);
                    }
                    if (refresh)
                        result.Refresh(Refresh.True);
                    return result;
                };

            IIndexResponse r = ElasticsearchDb.Client.Index(person, selector);
            return r.IsValid;
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
            JObject responseBody = JObject.Parse(response.Body.AsUtf8String());
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
            string result = searchResult.AsUtf8String();
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



        public static string MoreLikePeople(string[] ids)
        {
            var response =
                ElasticsearchDb.Client.LowLevel.Search<byte[]>("person", "person",
                    new PostData<object>(GetMoreLikeQuery(ids)));
            var jsonResponse = response.Body.AsUtf8String();
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

        private static string GetSearchQuery(PersonMatch apiSearch, int from, int size)
        {
            var mustClauses = new List<JObject>();
            if (apiSearch.Tags.Any())
                mustClauses.AddRange(apiSearch.Tags.Select("tags".Matching));
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
                postFilterClauses.AddRange(apiSearch.PostFilter.Tags.Select("tags".Matching));
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
            var filterTerms = tags.Select("tags".Matching).ToList();
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