using Newtonsoft.Json.Linq;

namespace Hdq.PersonDataManager.Api.Dal
{
    public static class QueryClauses
    {
        public static JObject Matching(this string property, string tag)
        {
            return JObject.Parse(
                @"{
                    ""match"" : {
                        " + property.Enclose() + ": " + tag.Enclose() + @"
                    }
                }"
            );
        }
    }
}