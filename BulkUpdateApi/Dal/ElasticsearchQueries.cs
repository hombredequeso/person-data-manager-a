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
                cmd.NewTag.Id.Value, cmd.NewTag.Value.Value,
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
                                        GetTermsFilterArray(tagIds)
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