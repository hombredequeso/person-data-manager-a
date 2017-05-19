using System;
using System.Collections.Generic;
using System.Linq;
using BulkUpdateApi.Api;
using BulkUpdateApi.Command;
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
                cmd.NewTag.Id.Value, 
                cmd.NewTag.Value.Value,
                cmd.CommandId);
            ElasticsearchResponse<byte[]> response = ElasticsearchDb.Client.LowLevel
                .UpdateByQuery<byte[]>(PersonIndex, bodyx);
            return response.Success;
        }

        public static bool UpdateMatchingPersonTags(BulkTagAdd requestBody)
        {
            PostData<object> bodyx = GetUpdate(
                requestBody.Match.Tags, 
                Int32.Parse(requestBody.AddTag.Id), 
                requestBody.AddTag.Value,
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
            var tagClauses = apiSearch.Tags.Select(ToTermTagId);
            var nameMatch = ToMatch("name", apiSearch.Name);
            var allCauses = tagClauses.Concat(new[] {nameMatch});
            var mustArrayClauses = new JArray(allCauses);

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

        public static JObject ToTermTagId(int tagId)
        {
            return new JObject
            {
                {
                    "term", new JObject
                    {
                        {"tags.id", tagId.ToString()}
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

        public static string GetUpdate(int[] tagIds, int tagId, string tagValue, Guid operationId)
        {
            var query = @"
                {
                    ""query"": {
                        ""constant_score"": {
                            ""filter"": {
                              ""bool"": {
                                ""must"":"
                                        + 
                                        new JArray(tagIds.Select(ToTermTagId))
                                        +
                              @"}
                            }
                        }
                    },
                  ""script"": {
                    ""inline"": "" " + GetScript() + @" "",
                    ""lang"": ""painless"",
                    ""params"": {
                      ""newtag"" :" + GetTag(tagId, tagValue) + @",
                      ""operationId"" : """ + operationId + @"""
                    }
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