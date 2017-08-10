using Hdq.PersonDataManager.Api.Dal;
using Nancy;
using Nancy.Extensions;
using Nancy.ModelBinding;

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
            Name = new Name();
            Tags = new string[0];
            PoolStatuses = new PoolStatus[0];
        }

        public Name Name { get; set; }
        public string[] Tags { get; set; }
        public PoolStatus[] PoolStatuses { get; set; }
    }


    public class SavedQueryMatch
    {
        public SavedQueryMatch()
        {
            Name = new Name();
            Tags = new string[0];
            PoolStatuses = new PoolStatus[0];
        }
        public Name Name { get; set; }
        public string[] Tags { get; set; }
        public PoolStatus[] PoolStatuses { get; set; }
    }

    public class SavedQuery
    {
        public MetaData Metadata { get; set; }
        public Parameters Query { get; set; }
    }

    public class QueryModule : NancyModule
    {
        public QueryModule()
        {
            Post["api/query"] = parameters =>
            {
                var query = this.Bind<SavedQuery>();
                var success = ElasticsearchQueries.IndexQuery(query);
                return success ? HttpStatusCode.Created : HttpStatusCode.InternalServerError;
            };

            Get["/api/query/{id}"] = parameters =>
            {
                string id = parameters.id;
                var queryFromElastic = ElasticsearchQueries.GetQuery(id);
                return queryFromElastic != null
                    ? Response.AsJson(queryFromElastic)
                    : HttpStatusCode.NotFound;
            };

            Post["/api/query/search"] = parameters =>
            {
                var eitherPager = RequestProcessor.GetPager(Request);
                return eitherPager.Match(
                    e => HttpStatusCode.BadRequest,
                    pager =>
                    {
                        var apiSearch2 = RequestProcessor.Deserialize<SavedQueryMatch>(Request.Body.AsString());
                        return apiSearch2.Match(
                            e => HttpStatusCode.BadRequest,
                            apiSearch =>
                            {
                                if (apiSearch.PoolStatuses == null)
                                {
                                    apiSearch.PoolStatuses = new PoolStatus[0];
                                }
                                var searchResult = ElasticsearchQueries.SearchSavedQueries(
                                    apiSearch,
                                    pager.From,
                                    pager.Size);
                                return !string.IsNullOrWhiteSpace(searchResult)
                                    ? Response.AsText(searchResult, "application/json")
                                    : HttpStatusCode.InternalServerError;
                            }
                        );
                    }
                );
            };
        }
    }
}
