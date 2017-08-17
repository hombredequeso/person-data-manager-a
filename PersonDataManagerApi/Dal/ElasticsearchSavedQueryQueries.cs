using System;
using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net;
using Hdq.PersonDataManager.Api.Modules;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Hdq.PersonDataManager.Api.Dal
{
    public static class ElasticsearchSavedQueryQueries
    {
        public static readonly string SavedQueryIndex = "savedquery";
        public static readonly string SavedQueryType = "savedquery";

        public static JObject GetQuery(string id)
        {
            ElasticsearchResponse<byte[]> response = ElasticsearchDb.Client.LowLevel.Get<byte[]>(
                SavedQueryIndex, SavedQueryType, id);
            if (response.Success)
            {
                var respBody = JObject.Parse(response.Body.AsUtf8String());
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

        public static bool IndexQuery(SavedQuery savedQuery, bool refresh)
        {
            // TODO: convert SavedQuery data into an actual query form here, and index that.
            string bodyContent = GetSavedQueryBody(savedQuery).ToString(Formatting.None);
            PostData<object> body = bodyContent;
            string id = savedQuery.Metadata.Id;
            Func<IndexRequestParameters, IndexRequestParameters> requestParams = p =>
            {
                if (refresh)
                    p.Refresh(Refresh.True);
                return p;
            };
            ElasticsearchResponse<byte[]> response = ElasticsearchDb.Client.LowLevel.Index<byte[]>(
                SavedQueryIndex,
                SavedQueryType,
                id,
                body,
                requestParams);
            return response.Success;
        }

        private static JObject GetSavedQueryBody(SavedQuery savedQuery)
        {
            var mustClauses = new List<JObject>();
            if (savedQuery.QueryParameters.Tags.Any())
                mustClauses.AddRange(savedQuery.QueryParameters.Tags.Select("tags".Matching));

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

        public static string SearchSavedQueries(SavedQueryMatch apiSearch, int from, int size)
        {
            var response = ElasticsearchDb.Client.LowLevel.Search<byte[]>(
                "savedquery",
                "savedquery",
                new PostData<object>(GetSavedQueriesSearchQuery(apiSearch, from, size)));

            return response.Body.AsUtf8String();
        }

        public static string PercolateSearchSavedQueries(SavedQueryPercolateMatch search, int from, int size)
        {
            var query = @"
                {
                  ""query"": {
                    ""percolate"": {
                        ""field"": ""query"",
                        ""document_type"": ""person"",
                        ""index"": " + search.Entity.Enclose() + @",
                        ""type"": " + search.Entity.Enclose() + @",
                        ""id"": """ + search.Id + @"""
                    }
                  } 
                }";

            var response =
                ElasticsearchDb.Client.LowLevel.Search<byte[]>("savedquery", "savedquery",
                    new PostData<object>(query));

            return response.Success 
                ? response.Body.AsUtf8String()
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

        
    }
}