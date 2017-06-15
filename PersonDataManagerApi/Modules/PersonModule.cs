
using System;
using System.Runtime.Remoting.Messaging;
using Hdq.PersonDataManager.Api.Dal;
using Hdq.PersonDataManager.Api.Domain;
using Hdq.PersonDataManager.Api.Lib;
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

    public static class DynamicParser
    {
        public static Either<int, HttpError> GetQueryParameter(dynamic x, int defaultValue)
        {
            if (!x.HasValue)
            {
                return defaultValue;
            }
            int i;
            if (!Int32.TryParse(x, out i))
                return new HttpError(HttpStatusCode.BadRequest, "");
            return i;
        }
    }
    
    public static class RequestProcessor
    {
        public static Either<Pager, HttpError> GetPager(Request r)
        {
            Either<int, HttpError> @from = DynamicParser.GetQueryParameter(r.Query["from"], 0);
            return @from.Match(f =>
                {
                    Either<int, HttpError> size1 = DynamicParser.GetQueryParameter(r.Query["size"], 10);
                    return size1.Match(
                        s => new Either<Pager, HttpError>(new Pager(f, s)),
                        e => new Either<Pager, HttpError>(new HttpError(HttpStatusCode.BadRequest, "")) );
                },
                e => new Either<Pager, HttpError>(new HttpError(HttpStatusCode.BadRequest, "")));
        }
    }

    public class Pager
    {
        public Pager(int @from, int size)
        {
            if (@from < 0)
                throw new ArgumentException("from cannot be negative");
            if (size < 0)
                throw new ArgumentException("size cannot be negative");
            From = @from;
            Size = size;
        }

        public int From { get; }
        public int Size { get; }
    }

    public class HttpError
    {
        public HttpError(HttpStatusCode statusCode, string errorMessage)
        {
            StatusCode = statusCode;
            ErrorMessage = errorMessage;
        }

        public HttpStatusCode StatusCode { get; }
        public string ErrorMessage { get; }
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
                return RequestProcessor.GetPager(Request).Match(
                    pager =>
                    {
                        var apiSearch = this.Bind<PersonMatch>();
                        var searchResult = ElasticsearchQueries.SearchPeople(
                            apiSearch,
                            pager.From,
                            pager.Size);
                        return !string.IsNullOrWhiteSpace(searchResult)
                            ? Response.AsText(searchResult, "application/json")
                            : HttpStatusCode.InternalServerError;
                    },
                    e => HttpStatusCode.BadRequest
                );
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