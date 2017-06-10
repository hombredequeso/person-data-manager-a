using System;
using Hdq.PersonDataManager.Api.Dal;
using Hdq.PersonDataManager.Api.Domain;
using Nancy;
using Nancy.ModelBinding;

namespace Hdq.PersonDataManager.Api.Modules
{
    public class Coord
    {
        public decimal Lat { get; set; }
        public decimal Lon { get; set; }
    }


    public class Name
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class ContactDetails
    {
        public PhoneContact[] Phone { get; set; }
        public EmailContact[] Email { get; set; }
        
    }

    public class PhoneContact
    {
        public string Label { get; set; }
        public string Number { get; set; }
    }

    public class EmailContact
    {
        public string Label { get; set; }
        public string Address { get; set; }
    }

    public class Person
    {
        public string Id { get; set; }
        public Name Name { get; set; }
        public string[] Tags { get; set; }
        public PoolStatus[] PoolStatuses { get; set; }
        public ContactDetails Contact { get; set; }
        public Address Address { get; set; }
    }

    public class Address
    {
        public string Region { get; set; }
        public Geo Geo { get; set; }
    }

    public class Geo
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
        public Name Name { get; set; }
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
                var cmd = new Command<BulkTagAdd>(Guid.NewGuid(), bulkTagAdd);
                ElasticsearchQueries.CommandResponse result = ElasticsearchQueries.UpdateMatchingPersonTags(cmd);
                return Response
                    .AsJson(ToResponseBody(result))
                    .WithStatusCode(result.Success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError);
            };
        }

        public static object ToResponseBody(ElasticsearchQueries.CommandResponse result)
        {
            return new
            {
                cmdId = result.CommandId,
                success = result.Success
            };
        }

    }
}