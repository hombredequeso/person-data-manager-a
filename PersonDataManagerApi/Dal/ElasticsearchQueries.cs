using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elasticsearch.Net;
using Hdq.PersonDataManager.Api.Domain;
using Hdq.PersonDataManager.Api.Modules;
using Nest;
using Newtonsoft.Json.Linq;

namespace Hdq.PersonDataManager.Api.Dal
{
    public static class StringExtensions
    {
        public static string Enclose(this string s, string e = "\"")
        {
            return $"{e}{s}{e}";
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

        public class CommandResponse
        {
            public CommandResponse(Guid commandId, bool success)
            {
                CommandId = commandId;
                Success = success;
            }

            public Guid CommandId { get; }
            public bool Success { get; }
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
            var responseBody = response.Body;
            var bodyAsStr = AsUtf8String(responseBody);
            Console.WriteLine(bodyAsStr);
            
            // JObject jobj = JObject.Parse(bodyAsStr);
            return new CommandResponse(cmd.Id, response.Success);
        }
        
        public static string SearchPeople(PersonMatch apiSearch, int from, int size)
        {
            var response =
                ElasticsearchDb.Client.LowLevel.Search<byte[]>("person", "person",
                    new PostData<object>(GetSearchQuery(apiSearch, from, size)));
            return response.Success ? AsUtf8String(response.Body) : null;
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
            var query = @"
                {
                  ""query"": {
                    ""more_like_this"": {
                      ""fields"" : [""tags""],
                        ""like"" : " + GetIdArray(ids) + @",
                        ""min_term_freq"":  1,
                        ""max_query_terms"": 12
                    }
                  }
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
                    + aggregations + @"
                }";
            return query;
        }

        public static bool SearchOnGeoCoord(PersonMatch apiSearch)
        {
            return (apiSearch?.Near?.Coord != null);
        }

        public static JProperty GetGeoAggregation(Coord coord)
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
            return new JProperty("geoAggregations", JObject.Parse(s));
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

        public static JProperty GetTagAggregationsB()
        {
            string value = 
                @"{
                    ""terms"" : {
                        ""field"": ""tags.keyword""
                    }
                }";  
            return new JProperty("tagAggs", JObject.Parse(value));
        }
        public static JObject AddIf(JObject o, bool conditional, Func<JProperty> p)
        {
            if (conditional)
                o.Add(p());
            return o;
        }

        public static JObject GetAggregations(PersonMatch apiSearch)
        {
            var aggs = new JObject();
            aggs.Add(GetTagAggregations());
            return AddIf(
                aggs, 
                SearchOnGeoCoord(apiSearch), 
                () => GetGeoAggregation(apiSearch.Near.Coord));
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