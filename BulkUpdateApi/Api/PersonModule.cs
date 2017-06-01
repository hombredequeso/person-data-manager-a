using BulkUpdateApi.Dal;
using Nancy;
using Nancy.ModelBinding;

namespace BulkUpdateApi.Api
{
    public class Coord
    {
        public decimal Lat { get; set; }
        public decimal Lon { get; set; }
    }

    public class Person
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string[] Tags { get; set; }
        public PoolStatus[] PoolStatuses { get; set; }
        public PersonGeoData Geo { get; set; }
    }

    public class PersonGeoData
    {
        public Coord Coord { get; set; }
    }

    public class GeoDistance
    {
        public Coord Coord { get; set; }
        public decimal Distance { get; set; }
    }

    public class PersonMatch
    {
        public string Name { get; set; }
        public string[] Tags { get; set; }
        public PoolStatus[] PoolStatuses { get; set; }
        public GeoDistance Near { get; set; }
    }

    public class PoolStatusMatch
    {
        public int PoolId { get; set; }
        public string Status { get; set; }
    }

    public class PoolStatus
    {
        public string Pool { get; set; }
        public string Status { get; set; }
    }

    public class BulkTagAdd
    {
        public PersonMatch Match { get; set; }
        public string AddTag { get; set; }
    }


    public class PersonModule : NancyModule
    {
        public PersonModule()
        {
            Get["/api/person/{id}"] = parameters =>
            {
                string id = parameters.id;
                var personFromElastic = ElasticsearchQueries.GetPerson(id);
                return personFromElastic != null
                    ? Response.AsJson(personFromElastic)
                    : HttpStatusCode.NotFound;
            };

            Post["api/person"] = parameters =>
            {
                var person = this.Bind<Person>();
                var success = ElasticsearchQueries.IndexPerson(person);
                return success ? HttpStatusCode.Created : HttpStatusCode.InternalServerError;
            };

            Post["/api/person/search"] = parameters =>
            {
                var apiSearch = this.Bind<PersonMatch>();
                var searchResult = ElasticsearchQueries.SearchPeople(apiSearch);
                return !string.IsNullOrWhiteSpace(searchResult)
                    ? Response.AsText(searchResult, "application/json")
                    : HttpStatusCode.InternalServerError;
            };

            Post["/api/person/morelike"] = parameters =>
            {
                var apiSearch = this.Bind<string[]>();
                var searchResult = ElasticsearchQueries.MoreLikePeople(apiSearch);
                return !string.IsNullOrWhiteSpace(searchResult)
                    ? Response.AsText(searchResult, "application/json")
                    : HttpStatusCode.InternalServerError;
            };

            Post["/api/person/tag/"] = parameters =>
            {
                var bulkTagAdd = this.Bind<BulkTagAdd>();
                var success = ElasticsearchQueries.UpdateMatchingPersonTags(bulkTagAdd);
                return success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
            };
        }
    }
}