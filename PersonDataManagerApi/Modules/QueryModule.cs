using Hdq.PersonDataManager.Api.Dal;
using Nancy;
using Nancy.Extensions;
using Nancy.ModelBinding;
using Newtonsoft.Json.Linq;

namespace Hdq.PersonDataManager.Api.Modules
{

    public class MetaData
    {
        public string Id { get; set; }
    }

    public class Parameters
    {
        public Parameters()
        {
            Tags = new string[0];
        }
        public string[] Tags { get; set; }
    }


    public class SavedQueryMatch
    {
    }

    public class SavedQueryPercolateMatch
    {
        public string Entity { get; set; } 
        public string Id { get; set; }
    }

    public class SavedQuery
    {
        public MetaData Metadata { get; set; }
        public Parameters QueryParameters { get; set; }
    }

    public class QueryModule : NancyModule
    {
        public QueryModule()
        {
            Post["api/query"] = parameters =>
            {
                var query = this.Bind<SavedQuery>();
                var isRefresh = Request.HasQueryParameter("refresh");
                var success = ElasticsearchQueries.IndexQuery(query, isRefresh);
                return success ? HttpStatusCode.Created : HttpStatusCode.InternalServerError;
            };

            Get["/api/query/{id}"] = parameters =>
            {
                string id = parameters.id;
                JObject queryFromElastic = ElasticsearchQueries.GetQuery(id);

                return queryFromElastic == null
                    ? HttpStatusCode.NotFound
                    : Response.AsText(queryFromElastic.ToString(), "application/json");

                // var response = (Response)(queryFromElastic.ToString());
                // response.ContentType = "application/json";
                // return response;
            };

            Post["/api/query/search"] = parameters =>
            {
                var eitherPager = RequestProcessor.GetPager(Request);
                return eitherPager.Match(
                    e => HttpStatusCode.BadRequest,
                    pager =>
                    {
                        var savedQueryMatch = RequestProcessor.Deserialize<SavedQueryMatch>(Request.Body.AsString());
                        return savedQueryMatch.Match(
                            e2 => HttpStatusCode.BadRequest,
                            percSearch => Search(percSearch, pager)
                        );
                    }
                );
            };


            Post["/api/query/searchPerc"] = parameters =>
            {
                var eitherPager = RequestProcessor.GetPager(Request);
                return eitherPager.Match(
                    e => HttpStatusCode.BadRequest,
                    pager =>
                    {
                        var percSearchEither = RequestProcessor.Deserialize<SavedQueryPercolateMatch>(
                            Request.Body.AsString());
                        return percSearchEither.Match(
                            e2 => HttpStatusCode.BadRequest,
                            percSearch => PercSearch(percSearch, pager)
                        );
                    }
                );
            };

        }

        private Response PercSearch(SavedQueryPercolateMatch percSearch, Pager pager)
        {
            var searchResult = ElasticsearchQueries.PercolateSearchSavedQueries(
                                    percSearch, pager.From, pager.Size);
            return !string.IsNullOrWhiteSpace(searchResult)
                ? Response.AsText(searchResult, "application/json")
                : HttpStatusCode.InternalServerError;
        }

        private Response Search(SavedQueryMatch apiSearch, Pager pager)
        {
            var searchResult = ElasticsearchQueries.SearchSavedQueries(
                apiSearch,
                pager.From,
                pager.Size);
            return !string.IsNullOrWhiteSpace(searchResult)
                ? Response.AsText(searchResult, "application/json")
                : HttpStatusCode.InternalServerError;
        }
    }
}
